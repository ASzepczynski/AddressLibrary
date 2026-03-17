using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do eksportowania danych z bazy do pliku Excel
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Eksportuje dane z DbSet do pliku Excel
        /// </summary>
        public async Task<string> ExportToExcelAsync<T>(
            IQueryable<T> query,
            string outputPath,
            string tableName,
            IProgress<ExportProgress>? progress = null) where T : class
        {
            progress?.Report(new ExportProgress
            {
                CurrentOperation = $"Pobieranie danych z tabeli {tableName}...",
                Stage = "Fetching"
            });

            // Pobierz dane z bazy
            var data = await query.ToListAsync();
            
            progress?.Report(new ExportProgress
            {
                CurrentOperation = $"Pobrano {data.Count} rekordów. Tworzenie pliku Excel...",
                TotalCount = data.Count,
                Stage = "Creating"
            });

            // Pobierz w³aœciwoœci typu T
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            var fileName = $"{tableName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var fullPath = Path.Combine(outputPath, fileName);

            // Utwórz plik Excel
            using (var document = SpreadsheetDocument.Create(fullPath, SpreadsheetDocumentType.Workbook))
            {
                // Dodaj workbook
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                // Dodaj worksheet
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                // Dodaj arkusz do workbook
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet()
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = tableName
                };
                sheets.Append(sheet);

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

                // WIERSZ 1: Nag³ówki
                var headerRow = new Row { RowIndex = 1 };
                uint columnIndex = 1;

                foreach (var prop in properties)
                {
                    var cell = new Cell
                    {
                        CellReference = GetColumnLetter(columnIndex) + "1",
                        DataType = CellValues.String,
                        CellValue = new CellValue(prop.Name)
                    };
                    headerRow.Append(cell);
                    columnIndex++;
                }

                sheetData.Append(headerRow);

                progress?.Report(new ExportProgress
                {
                    CurrentOperation = "Zapisywanie danych do pliku Excel...",
                    TotalCount = data.Count,
                    Stage = "Writing"
                });

                // WIERSZE 2+: Dane
                uint rowIndex = 2;
                int processedCount = 0;

                foreach (var item in data)
                {
                    var dataRow = new Row { RowIndex = rowIndex };
                    columnIndex = 1;

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item);
                        var cellValue = value?.ToString() ?? "";

                        var cell = new Cell
                        {
                            CellReference = GetColumnLetter(columnIndex) + rowIndex.ToString(),
                            DataType = CellValues.String,
                            CellValue = new CellValue(cellValue)
                        };
                        dataRow.Append(cell);
                        columnIndex++;
                    }

                    sheetData.Append(dataRow);
                    rowIndex++;
                    processedCount++;

                    // Raportuj postêp co 1000 wpisów
                    if (processedCount % 1000 == 0)
                    {
                        progress?.Report(new ExportProgress
                        {
                            CurrentOperation = $"Zapisano {processedCount}/{data.Count} wierszy...",
                            TotalCount = data.Count,
                            ProcessedCount = processedCount,
                            Stage = "Writing"
                        });
                    }
                }

                progress?.Report(new ExportProgress
                {
                    CurrentOperation = "Finalizowanie pliku Excel...",
                    TotalCount = data.Count,
                    ProcessedCount = data.Count,
                    Stage = "Finalizing"
                });

                workbookPart.Workbook.Save();
            }

            progress?.Report(new ExportProgress
            {
                CurrentOperation = $"Zakoñczono! Plik: {fileName}",
                TotalCount = data.Count,
                ProcessedCount = data.Count,
                IsCompleted = true,
                OutputFileName = fileName
            });

            return fullPath;
        }

        /// <summary>
        /// Konwertuje numer kolumny na literê (1 -> A, 2 -> B, ..., 27 -> AA)
        /// </summary>
        private string GetColumnLetter(uint columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                uint modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }
    }

    /// <summary>
    /// Postêp eksportu
    /// </summary>
    public class ExportProgress
    {
        public string CurrentOperation { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string OutputFileName { get; set; } = string.Empty;
    }
}