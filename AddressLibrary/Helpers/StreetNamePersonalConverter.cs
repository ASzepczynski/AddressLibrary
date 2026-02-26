using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Konwerter nazw ulic osobowych z pliku Excel (Updates/UliceOsobowe.xlsx)
    /// </summary>
    public class StreetNamePersonalConverter
    {
        private readonly Dictionary<(string Cecha, string Original), (string Cecha, string Nazwa1, string Nazwa2)> _conversionDict;

        public StreetNamePersonalConverter(string appDataPath)
        {
            // ✅ POPRAWKA: Usuń StringComparer - użyjemy custom comparera w metodzie TryConvert
            _conversionDict = new Dictionary<(string, string), (string, string, string)>();
            LoadFromExcel(appDataPath);
        }

        private void LoadFromExcel(string appDataPath)
        {
            var excelPath = Path.Combine(appDataPath, "AppData", "Updates", "UliceOsobowe.xlsx");

            if (!File.Exists(excelPath))
            {
                throw new Exception("Plik {excelPath} nie istnieje");
                // Plik nie istnieje - konwerter będzie pusty (bezpieczne)
            }

            using var document = SpreadsheetDocument.Open(excelPath, false);
            var workbookPart = document.WorkbookPart;
            var worksheetPart = workbookPart?.WorksheetParts.First();
            var sheetData = worksheetPart?.Worksheet.Elements<SheetData>().First();

            if (sheetData == null)
                return;

            var rows = sheetData.Elements<Row>().Skip(1); // Pomiń nagłówek

            foreach (var row in rows)
            {
                var cells = row.Elements<Cell>().ToList();

                if (cells.Count < 5)
                    continue;

                var stara_cecha = GetCellValue(workbookPart, cells[0]);
                var nowa_cecha = GetCellValue(workbookPart, cells[1]);
                var nazwa1 = GetCellValue(workbookPart, cells[2]);
                var nazwa2 = GetCellValue(workbookPart, cells[3]);
                var original = GetCellValue(workbookPart, cells[4]);

                if (!string.IsNullOrWhiteSpace(original))
                {
                    // ✅ POPRAWKA: Normalizuj do lowercase przy zapisie (dla case-insensitive)
                    var key = (stara_cecha.Trim().ToLowerInvariant(), original.Trim().ToLowerInvariant());
                    var value = (nowa_cecha.Trim(), nazwa1.Trim(), nazwa2.Trim());

                    // Dodaj lub nadpisz (ostatnia wartość wygrywa)
                    _conversionDict[key] = value;
                }
            }
        }

        private string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell == null || cell.CellValue == null)
                return string.Empty;

            var value = cell.CellValue.InnerText;

            // Sprawdź czy to SharedString
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
        /// Próbuje skonwertować nazwę ulicy używając słownika z Excel
        /// </summary>
        /// <param name="cecha">Cecha ulicy (np. "ul.")</param>
        /// <param name="nazwa1">Nazwa główna z TerytUlic</param>
        /// <param name="nazwa2">Nazwa dodatkowa z TerytUlic</param>
        /// <param name="convertedCecha">Skonwertowana nazwa główna</param>
        /// <param name="convertedNazwa1">Skonwertowana nazwa główna</param>
        /// <param name="convertedNazwa2">Skonwertowana nazwa dodatkowa</param>
        /// <returns>True jeśli znaleziono konwersję</returns>
        public bool TryConvert(
            string cecha,
            string nazwa1,
            string nazwa2,
            out string convertedCecha,
            out string convertedNazwa1,
            out string convertedNazwa2)
        {
            // Budujemy "oryginał" tak jak w pliku Excel: nazwa2 + " " + nazwa1
            var original = string.IsNullOrWhiteSpace(nazwa2)
                ? nazwa1
                : $"{nazwa2} {nazwa1}";

            // ✅ POPRAWKA: Normalizuj do lowercase dla case-insensitive porównania
            var key = (cecha.Trim().ToLowerInvariant(), original.Trim().ToLowerInvariant());

            if (_conversionDict.TryGetValue(key, out var converted))
            {
                convertedNazwa1 = converted.Nazwa1;
                convertedNazwa2 = converted.Nazwa2;
                convertedCecha = converted.Cecha;
                return true;
            }
            convertedCecha = cecha;
            convertedNazwa1 = nazwa1;
            convertedNazwa2 = nazwa2;
            return false;
        }

        // 🆕 Metoda do debugowania
        public IEnumerable<(string Cecha, string Original)> GetAllKeys()
        {
            return _conversionDict.Keys;
        }

        public int Count => _conversionDict.Count;
    }
}