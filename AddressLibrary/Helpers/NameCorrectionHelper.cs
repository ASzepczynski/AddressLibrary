using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Helper do wczytywania i stosowania korekt nazw miast i ulic z pliku Excel (AppData/Updates/Korekty.xlsx)
    /// Format pliku: Typ | Stara nazwa | Nowa nazwa
    /// Typ: M (miasto), U (ulica)
    /// </summary>
    public class NameCorrectionHelper
    {
        private readonly Dictionary<(string Type, string OldName), string> _corrections;

        public NameCorrectionHelper(string appDataPath)
        {
            _corrections = new Dictionary<(string, string), string>(new CorrectionKeyComparer());
            LoadFromExcel(appDataPath);
        }

        /// <summary>
        /// Wczytuje korekty z pliku Excel
        /// </summary>
        private void LoadFromExcel(string appDataPath)
        {
            var excelPath = Path.Combine(appDataPath, "AppData", "Updates", "KorektyNazw.xlsx");

            if (!File.Exists(excelPath))
            {
                // Plik nie istnieje 
                Console.WriteLine($"[NameCorrectionHelper] Plik korekt nie istnieje: {excelPath}");
                return; 
            }

            using var document = SpreadsheetDocument.Open(excelPath, false);
            var workbookPart = document.WorkbookPart;
            var worksheetPart = workbookPart?.WorksheetParts.First();
            var sheetData = worksheetPart?.Worksheet.Elements<SheetData>().First();

            if (sheetData == null)
                return;

            var rows = sheetData.Elements<Row>().Skip(1); // Pomiñ nag³ówek

            foreach (var row in rows)
            {
                var cells = row.Elements<Cell>().ToList();

                if (cells.Count < 3)
                    continue;

                var type = GetCellValue(workbookPart, cells[0]).Trim().ToUpperInvariant();
                var oldName = GetCellValue(workbookPart, cells[1]).Trim();
                var newName = GetCellValue(workbookPart, cells[2]).Trim();

                // Walidacja typu
                if (type != "M" && type != "U")
                    continue;

                if (string.IsNullOrWhiteSpace(oldName))
                    continue;

                var key = (type, oldName);
                _corrections[key] = newName;
            }
        }

        /// <summary>
        /// Pobiera wartoœæ komórki Excel (obs³uguje SharedString)
        /// </summary>
        private string GetCellValue(WorkbookPart? workbookPart, Cell cell)
        {
            if (cell == null || cell.CellValue == null || workbookPart == null)
                return string.Empty;

            var value = cell.CellValue.InnerText;

            // SprawdŸ czy to SharedString
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTable = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                if (stringTable != null)
                {
                    value = stringTable.SharedStringTable
                        .ElementAt(int.Parse(value))
                        .InnerText;
                }
            }

            return value ?? string.Empty;
        }

        /// <summary>
        /// Próbuje zastosowaæ korektê nazwy
        /// </summary>
        /// <param name="type">Typ korekty: "M" (miasto) lub "U" (ulica)</param>
        /// <param name="oldName">Stara nazwa do sprawdzenia</param>
        /// <param name="newName">Nowa nazwa (parametr wyjœciowy)</param>
        /// <returns>True jeœli znaleziono korektê, false w przeciwnym razie</returns>
        public bool TryCorrect(string type, string? oldName, out string? newName)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(oldName))
            {
                newName = oldName ?? string.Empty;
                return false;
            }

            var normalizedType = type.Trim().ToUpperInvariant();
            var normalizedOldName = oldName.Trim();

            var key = (normalizedType, normalizedOldName);

            if (_corrections.TryGetValue(key, out var corrected))
            {
                newName = corrected;
                return true;
            }

            newName = oldName;
            return false;
        }

        /// <summary>
        /// Zwraca liczbê za³adowanych korekt
        /// </summary>
        public int Count => _corrections.Count;

        /// <summary>
        /// Zwraca liczbê korekt dla danego typu
        /// </summary>
        public int GetCountByType(string type)
        {
            var normalizedType = type.Trim().ToUpperInvariant();
            return _corrections.Keys.Count(k => k.Item1 == normalizedType);
        }

        /// <summary>
        /// Custom comparer dla kluczy s³ownika (case-insensitive dla starej nazwy)
        /// </summary>
        private class CorrectionKeyComparer : IEqualityComparer<(string Type, string OldName)>
        {
            public bool Equals((string Type, string OldName) x, (string Type, string OldName) y)
            {
                return string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.OldName, y.OldName, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string Type, string OldName) obj)
            {
                return HashCode.Combine(
                    obj.Type?.ToLowerInvariant(),
                    obj.OldName?.ToLowerInvariant()
                );
            }
        }
    }
}