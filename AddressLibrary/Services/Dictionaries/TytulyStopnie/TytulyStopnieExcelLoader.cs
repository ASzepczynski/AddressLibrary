using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.Dictionaries.TytulyStopnie
{
    /// <summary>
    /// Serwis do ³adowania s³ownika TytulyStopnie z pliku Excel do bazy danych
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
        /// £aduje dane z pliku Excel TytulyStopnie.xlsx do tabeli TytulyStopnie
        /// Struktura kolumn:
        /// A = Nazwa (pe³na nazwa, np. "genera³")
        /// B = Dopelniacz (forma dope³niacza, np. "genera³a")
        /// C = Skrot (skrót, np. "gen.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            var excelPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", "TytulyStopnie.xlsx");

            _logger.LogInfo("=== Rozpoczêcie ³adowania TytulyStopnie ===");

            try
            {
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

                var tytulyFromExcel = new List<TytulStopien>();

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
                        var dopelniacz = cellValues.GetValueOrDefault("B")?.Trim();
                        var skrot = cellValues.GetValueOrDefault("C")?.Trim();

                        if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot) && !string.IsNullOrWhiteSpace(dopelniacz))
                        {
                            tytulyFromExcel.Add(new TytulStopien
                            {
                                Nazwa = nazwa,
                                Skrot = skrot,
                                Dopelniacz = dopelniacz
                            });
                        }
                    }
                }

                result.TotalCount = tytulyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Aktualizacja bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Aktualizuj bazê danych - UPSERT
                foreach (var tytul in tytulyFromExcel)
                {
                    var existing = await _context.TytulyStopnie
                        .FirstOrDefaultAsync(t => t.Nazwa == tytul.Nazwa);

                    if (existing != null)
                    {
                        existing.Skrot = tytul.Skrot;
                        existing.Dopelniacz = tytul.Dopelniacz;
                        result.UpdatedCount++;
                    }
                    else
                    {
                        await _context.TytulyStopnie.AddAsync(tytul);
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
            var defaultRecord = await _context.TytulyStopnie
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == -1);

            if (defaultRecord == null)
            {
                _logger.LogInfo("Dodawanie domyœlnego rekordu z ID = -1");

                try
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT TytulyStopnie ON;
                        
                        IF NOT EXISTS (SELECT 1 FROM TytulyStopnie WHERE Id = -1)
                        BEGIN
                            INSERT INTO TytulyStopnie (Id, Nazwa, Skrot, Dopelniacz) 
                            VALUES (-1, 'brak', '', 'braku');
                        END
                        
                        SET IDENTITY_INSERT TytulyStopnie OFF;
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