using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.Dictionaries.CechyUlic
{
    /// <summary>
    /// Serwis do ³adowania s³ownika CechyUlic z pliku Excel do bazy danych
    /// </summary>
    public class CechyUlicExcelLoader
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly GeneralLogger _logger;

        public CechyUlicExcelLoader(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new GeneralLogger(appDataPath, "LoadCechyUlic.txt", "Log CechyUlic");
        }

        /// <summary>
        /// £aduje dane z pliku Excel CechyUlic.xlsx do tabeli CechyUlic
        /// Struktura kolumn:
        /// A = Nazwa (pe³na nazwa, np. "ulica")
        /// B = Skrot (skrót, np. "ul.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            var excelPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", "CechyUlic.xlsx");

            _logger.LogInfo("=== Rozpoczêcie ³adowania CechyUlic ===");

            try
            {
                // Upewnij siê, ¿e rekord z ID = -1 istnieje
                await EnsureDefaultRecordExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"B³¹d podczas dodawania domyœlnego rekordu: {ex.Message}");
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
                    CurrentOperation = $"Aktualizacja bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Aktualizuj bazê danych - UPSERT
                foreach (var cecha in cechyFromExcel)
                {
                    var existing = await _context.CechyUlic
                        .FirstOrDefaultAsync(c => c.Nazwa == cecha.Nazwa);

                    if (existing != null)
                    {
                        existing.Skrot = cecha.Skrot;
                        result.UpdatedCount++;
                    }
                    else
                    {
                        await _context.CechyUlic.AddAsync(cecha);
                        result.InsertedCount++;
                    }

                    result.ProcessedCount++;

                    if (result.ProcessedCount % 10 == 0 || result.ProcessedCount == result.TotalCount)
                    {
                        progress?.Report(new LoadProgress
                        {
                            CurrentOperation = $"Przetworzono: {result.ProcessedCount}/{result.TotalCount}",
                            TotalCount = result.TotalCount,
                            ProcessedCount = result.ProcessedCount
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInfo($"Zakoñczono: Dodano: {result.InsertedCount}, Zaktualizowano: {result.UpdatedCount}");

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

        private async Task EnsureDefaultRecordExistsAsync()
        {
            var defaultRecord = await _context.CechyUlic
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == -1);

            if (defaultRecord == null)
            {
                _logger.LogInfo("Dodawanie domyœlnego rekordu z ID = -1");

                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT CechyUlic ON;
                        
                        IF NOT EXISTS (SELECT 1 FROM CechyUlic WHERE Id = -1)
                        BEGIN
                            INSERT INTO CechyUlic (Id, Nazwa, Skrot) 
                            VALUES (-1, 'brak', '');
                        END
                        
                        SET IDENTITY_INSERT CechyUlic OFF;
                    ");

                    _logger.LogInfo("Dodano domyœlny rekord z ID = -1");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"B³¹d podczas dodawania rekordu: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _logger.LogInfo("Domyœlny rekord z ID = -1 ju¿ istnieje");
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