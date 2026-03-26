using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Helpers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.Dictionaries.TytulyStopnie
{
    /// <summary>
    /// Serwis do ładowania słownika TytulyStopnie z pliku Excel do bazy danych
    /// </summary>
    public class TytulyStopnieExcelLoader
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly GeneralLogger _logger;

        public TytulyStopnieExcelLoader(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new GeneralLogger(appDataPath, "LoadTytulyStopnie.txt", "Log TytulyStopnie");
        }

        /// <summary>
        /// Ładuje dane z pliku Excel TytulyStopnie.xlsx do tabeli TytulyStopnie
        /// Struktura kolumn:
        /// A = Nazwa (pełna nazwa, np. "generał")
        /// B = Dopelniacz (forma dopełniacza, np. "generała")
        /// C = Skrot (skrót, np. "gen.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            var excelPath = Directories.GetExcelFilePath("TytulyStopnie.xlsx");

            _logger.LogInfo("=== Rozpoczęcie ładowania TytulyStopnie ===");

            try
            {
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
                var deletedCount = await _context.TytulyStopnie
                    .Where(t => t.Id != -1)
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

                var tytulyFromExcel = new List<TytulStopien>();
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
                        var dopelniacz = cellValues.GetValueOrDefault("B")?.Trim();
                        var skrot = cellValues.GetValueOrDefault("C")?.Trim();

                        // Logowanie wczytanych wartości
                        _logger.LogInfo($"Wiersz {rowNumber}: A(Nazwa)='{nazwa}', B(Dopelniacz)='{dopelniacz}', C(Skrot)='{skrot}'");

                        if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot) && !string.IsNullOrWhiteSpace(dopelniacz))
                        {
                            tytulyFromExcel.Add(new TytulStopien
                            {
                                Nazwa = nazwa,
                                Skrot = skrot,
                                Dopelniacz = dopelniacz
                            });
                        }
                        else
                        {
                            _logger.LogWarning($"Wiersz {rowNumber}: Pominięto - brak wymaganych danych");
                        }
                    }
                }

                result.TotalCount = tytulyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

                // Wyświetl wszystkie wczytane rekordy
                _logger.LogInfo("=== Lista wczytanych tytułów ===");
                foreach (var t in tytulyFromExcel)
                {
                    _logger.LogInfo($"  Nazwa='{t.Nazwa}', Dopelniacz='{t.Dopelniacz}', Skrot='{t.Skrot}'");
                }

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Dodawanie do bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Dodaj nowe rekordy do bazy
                await _context.TytulyStopnie.AddRangeAsync(tytulyFromExcel);
                await _context.SaveChangesAsync();

                result.InsertedCount = tytulyFromExcel.Count;
                result.ProcessedCount = tytulyFromExcel.Count;

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
            await DefaultRecordHelper.EnsureTytulStopienDefaultAsync(_context, _logger);
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