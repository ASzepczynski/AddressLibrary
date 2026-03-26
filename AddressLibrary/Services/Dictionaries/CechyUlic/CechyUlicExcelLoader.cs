using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.Dictionaries.CechyUlic
{
    /// <summary>
    /// Serwis do ładowania słownika CechyUlic z pliku Excel do bazy danych
    /// </summary>
    public class CechyUlicExcelLoader
    {
        private readonly AddressDbContext _context;
        private readonly GeneralLogger _logger;

        public CechyUlicExcelLoader(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _logger = new GeneralLogger(appDataPath, "LoadCechyUlic.txt", "Log CechyUlic");
        }

        /// <summary>
        /// Ładuje dane z pliku Excel CechyUlic.xlsx do tabeli CechyUlic
        /// Plik Excel znajduje się w AddressLibrary/AppData/Dictionaries/
        /// Struktura kolumn:
        /// A = Nazwa (pełna nazwa, np. "ulica")
        /// B = Skrot (skrót, np. "ul.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            
            // ✅ POPRAWKA: Szukaj pliku w AddressLibrary/AppData/Dictionaries/
            var excelPath = Directories.GetExcelFilePath("CechyUlic.xlsx");

            _logger.LogInfo("=== Rozpoczęcie ładowania CechyUlic ===");
            _logger.LogInfo($"Ścieżka do pliku Excel: {excelPath}");

            try
            {
                // Upewnij się, że rekord z ID = -1 istnieje
                await EnsureDefaultRecordExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas dodawania domyślnego rekordu: {ex.Message}");
            }

            try
            {
                // Usuń wszystkie rekordy oprócz Id = -1
                _logger.LogInfo("Usuwanie istniejących rekordów (oprócz Id = -1)...");
                var deletedCount = await _context.CechyUlic
                    .Where(c => c.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usunięto {deletedCount} rekordów");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usunięto {deletedCount} starych rekordów"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas usuwania rekordów: {ex.Message}");
                result.ErrorMessage = $"Błąd podczas usuwania rekordów: {ex.Message}";
                return result;
            }

            if (!File.Exists(excelPath))
            {
                _logger.LogError($"Plik nie istnieje: {excelPath}");
                result.ErrorMessage = $"Plik nie istnieje: {excelPath}";
                return result;
            }

            try
            {
                progress?.Report(new LoadProgress { CurrentOperation = "Odczyt pliku Excel..." });

                var cechyFromExcel = new List<CechaUlicy>();
                int rowNumber = 0;

                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(excelPath, false))
                {
                    WorkbookPart? workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart == null)
                    {
                        _logger.LogError("Nie można otworzyć arkusza Excel");
                        result.ErrorMessage = "Nie można otworzyć arkusza Excel";
                        return result;
                    }

                    string[] sharedStrings = Array.Empty<string>();
                    var sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sharedStringPart?.SharedStringTable != null)
                    {
                        sharedStrings = sharedStringPart.SharedStringTable
                            .Elements<SharedStringItem>()
                            .Select(item => item.InnerText)
                            .ToArray();
                    }

                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    bool isFirstRow = true;

                    foreach (var row in sheetData.Elements<Row>())
                    {
                        rowNumber++;

                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            _logger.LogInfo($"Wiersz {rowNumber}: NAGŁÓWEK (pomijam)");
                            continue;
                        }

                        var cellValues = GetRowCellsDictionary(row, sharedStrings);

                        var nazwa = cellValues.GetValueOrDefault("A")?.Trim();
                        var skrot = cellValues.GetValueOrDefault("B")?.Trim();

                        // Logowanie wczytanych wartości
                        _logger.LogInfo($"Wiersz {rowNumber}: A(Nazwa)='{nazwa}', B(Skrot)='{skrot}'");

                        if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot))
                        {
                            cechyFromExcel.Add(new CechaUlicy
                            {
                                Nazwa = nazwa,
                                Skrot = skrot
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"Wiersz {rowNumber}: Pominięto - brak wymaganych danych");
                        }
                    }
                }

                result.TotalCount = cechyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

                // Wyświetl wszystkie wczytane rekordy
                _logger.LogInfo("=== Lista wczytanych cech ulic ===");
                foreach (var c in cechyFromExcel)
                {
                    _logger.LogInfo($"  Nazwa='{c.Nazwa}', Skrot='{c.Skrot}'");
                }

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Dodawanie do bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Dodaj nowe rekordy do bazy
                await _context.CechyUlic.AddRangeAsync(cechyFromExcel);
                await _context.SaveChangesAsync();

                result.InsertedCount = cechyFromExcel.Count;
                result.ProcessedCount = cechyFromExcel.Count;

                _logger.LogInfo($"Zakończono: Dodano: {result.InsertedCount} nowych rekordów");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = "Zakończono",
                    TotalCount = result.TotalCount,
                    ProcessedCount = result.ProcessedCount,
                    IsCompleted = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }


        private async Task EnsureDefaultRecordExistsAsync()
        {
            await DefaultRecordHelper.EnsureCechaUlicyDefaultAsync(_context, _logger);
        }

        private static Dictionary<string, string> GetRowCellsDictionary(Row row, string[] sharedStrings)
        {
            var result = new Dictionary<string, string>();

            foreach (var cell in row.Elements<Cell>())
            {
                var columnName = GetColumnName(cell.CellReference?.Value);
                if (!string.IsNullOrEmpty(columnName))
                {
                    var value = GetCellValue(cell, sharedStrings);
                    if (value != null)
                    {
                        result[columnName] = value;
                    }
                }
            }

            return result;
        }

        private static string GetColumnName(string? cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
                return string.Empty;

            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        private static string? GetCellValue(Cell cell, string[] sharedStrings)
        {
            if (cell.CellValue == null)
                return null;

            var value = cell.CellValue.Text;

            if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out int stringIndex))
            {
                if (stringIndex >= 0 && stringIndex < sharedStrings.Length)
                {
                    return sharedStrings[stringIndex];
                }
            }

            return value;
        }
    }
}