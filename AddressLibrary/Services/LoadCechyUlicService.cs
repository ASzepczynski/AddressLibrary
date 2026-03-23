using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do ³adowania s³ownika CechyUlic z pliku Excel do bazy danych
    /// </summary>
    public class LoadCechyUlicService
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly GeneralLogger _logger;

        public LoadCechyUlicService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new GeneralLogger(appDataPath,  "LoadCechyUlic.txt", "Log CechyUlic");
        }

        /// <summary>
        /// £aduje dane z pliku Excel CechyUlic.xlsx do tabeli CechyUlic
        /// Struktura kolumn:
        /// A = Nazwa (pe³na nazwa, np. "ulica")
        /// B = Skrot (skrót, np. "ul.")
        /// </summary>
        public async Task<LoadResult> LoadAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            var excelPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", "CechyUlic.xlsx");

            _logger.LogInfo("=== Rozpoczêcie ³adowania CechyUlic ===");

            try
            {
                // Upewnij siê, ¿e rekord z ID = -1 istnieje
                await DefaultRecordHelper.EnsureCechaUlicyDefaultAsync(_context, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError($"B³¹d podczas dodawania domyœlnego rekordu: {ex.Message}");
                // Kontynuuj mimo b³êdu - mo¿e rekord ju¿ istnieje
            }

            try
            {
                // Usuñ wszystkie rekordy oprócz Id = -1
                _logger.LogInfo("Usuwanie istniej¹cych rekordów (oprócz Id = -1)...");
                var deletedCount = await _context.CechyUlic
                    .Where(c => c.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usuniêto {deletedCount} rekordów");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usuniêto {deletedCount} starych rekordów"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"B³¹d podczas usuwania rekordów: {ex.Message}");
                result.ErrorMessage = $"B³¹d podczas usuwania rekordów: {ex.Message}";
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

                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(excelPath, false))
                {
                    WorkbookPart? workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart == null)
                    {
                        _logger.LogError("Nie mo¿na otworzyæ arkusza Excel");
                        result.ErrorMessage = "Nie mo¿na otworzyæ arkusza Excel";
                        return result;
                    }

                    // Za³aduj SharedStringTable
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
                        // Pomiñ nag³ówek
                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            continue;
                        }

                        var cellValues = GetRowCellsDictionary(row, sharedStrings);

                        var nazwa = cellValues.GetValueOrDefault("A")?.Trim();
                        var skrot = cellValues.GetValueOrDefault("B")?.Trim();

                        if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot))
                        {
                            cechyFromExcel.Add(new CechaUlicy
                            {
                                Nazwa = nazwa,
                                Skrot = skrot
                            });
                        }
                    }
                }

                result.TotalCount = cechyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

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

                _logger.LogInfo($"Zakoñczono: Dodano: {result.InsertedCount} nowych rekordów");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = "Zakoñczono",
                    TotalCount = result.TotalCount,
                    ProcessedCount = result.ProcessedCount,
                    IsCompleted = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"B³¹d: {ex.Message}");
                result.ErrorMessage = ex.Message;
                return result;
            }
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