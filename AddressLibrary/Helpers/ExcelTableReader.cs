using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Uniwersalny czytnik plików Excel (.xlsx).
    /// Pierwszy wiersz arkusza traktowany jest jako nag³ówki kolumn.
    /// Ka¿dy kolejny wiersz jest dostêpny jako <see cref="ExcelRow"/>,
    /// który pozwala odwo³ywaæ siê do wartoœci po nazwie kolumny.
    /// </summary>
    /// <example>
    /// var rows = ExcelTableReader.Read(@"C:\dane\plik.xlsx");
    /// foreach (var row in rows)
    /// {
    ///     var nazwa  = row["Nazwa"];
    ///     var kwota  = row.GetDecimal("Kwota");
    ///     var aktywny = row.GetBool("Aktywny");
    /// }
    /// </example>
    public static class ExcelTableReader
    {
        /// <summary>
        /// Wczytuje arkusz Excel i zwraca listê wierszy indeksowanych nazwami nag³ówków.
        /// </summary>
        /// <param name="filePath">Pe³na œcie¿ka do pliku .xlsx</param>
        /// <param name="sheetIndex">Indeks arkusza (0 = pierwszy), domyœlnie 0</param>
        /// <returns>Lista wierszy danych (bez wiersza nag³ówkowego)</returns>
        /// <exception cref="FileNotFoundException">Gdy plik nie istnieje</exception>
        /// <exception cref="InvalidOperationException">Gdy arkusz jest pusty lub brak nag³ówków</exception>
        public static List<ExcelRow> Read(string filePath, int sheetIndex = 0)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Plik Excel nie istnieje: {filePath}", filePath);

            using var spreadsheet = SpreadsheetDocument.Open(filePath, isEditable: false);

            var workbookPart = spreadsheet.WorkbookPart
                ?? throw new InvalidOperationException("Plik Excel nie zawiera arkusza roboczego.");

            // Za³aduj tablicê wspó³dzielonych ci¹gów (SharedStrings)
            var sharedStrings = LoadSharedStrings(workbookPart);

            // Wybierz arkusz wg indeksu
            var worksheetParts = workbookPart.WorksheetParts.ToList();
            if (sheetIndex >= worksheetParts.Count)
                throw new InvalidOperationException(
                    $"Arkusz o indeksie {sheetIndex} nie istnieje. Plik zawiera {worksheetParts.Count} arkusz(e/y).");

            var sheetData = worksheetParts[sheetIndex]
                .Worksheet
                .Elements<SheetData>()
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Arkusz nie zawiera danych.");

            var allRows = sheetData.Elements<Row>().ToList();
            if (allRows.Count == 0)
                throw new InvalidOperationException("Arkusz jest pusty.");

            // Wiersz 0: nag³ówki ? buduj mapê kolumnaLitera ? nazwaKolumny
            var headers = ParseHeaders(allRows[0], sharedStrings);
            if (headers.Count == 0)
                throw new InvalidOperationException("Wiersz nag³ówkowy jest pusty.");

            // Pozosta³e wiersze: dane
            var result = new List<ExcelRow>(allRows.Count - 1);
            for (int i = 1; i < allRows.Count; i++)
            {
                var cellValues = ReadRowCells(allRows[i], sharedStrings);

                // Zamieñ literê kolumny na nazwê nag³ówka
                var namedValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (colLetter, header) in headers)
                {
                    namedValues[header] = cellValues.GetValueOrDefault(colLetter);
                }

                result.Add(new ExcelRow(namedValues, i + 1));
            }

            return result;
        }

        // ?? Metody prywatne ???????????????????????????????????????????????????

        private static string[] LoadSharedStrings(WorkbookPart workbookPart)
        {
            var part = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
            return part?.SharedStringTable
                       .Elements<SharedStringItem>()
                       .Select(item => item.InnerText)
                       .ToArray()
                   ?? Array.Empty<string>();
        }

        private static Dictionary<string, string> ParseHeaders(Row headerRow, string[] sharedStrings)
        {
            var headers = new Dictionary<string, string>();
            foreach (var cell in headerRow.Elements<Cell>())
            {
                var col = GetColumnLetter(cell.CellReference?.Value);
                var value = GetCellValue(cell, sharedStrings)?.Trim();
                if (!string.IsNullOrEmpty(col) && !string.IsNullOrEmpty(value))
                    headers[col] = value;
            }
            return headers;
        }

        private static Dictionary<string, string?> ReadRowCells(Row row, string[] sharedStrings)
        {
            var cells = new Dictionary<string, string?>();
            foreach (var cell in row.Elements<Cell>())
            {
                var col = GetColumnLetter(cell.CellReference?.Value);
                if (!string.IsNullOrEmpty(col))
                    cells[col] = GetCellValue(cell, sharedStrings);
            }
            return cells;
        }

        private static string GetColumnLetter(string? cellReference)
        {
            if (string.IsNullOrEmpty(cellReference)) return string.Empty;
            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        private static string? GetCellValue(Cell cell, string[] sharedStrings)
        {
            if (cell.CellValue == null) return null;

            var raw = cell.CellValue.Text;

            if (cell.DataType?.Value == CellValues.SharedString
                && int.TryParse(raw, out int idx)
                && idx >= 0 && idx < sharedStrings.Length)
            {
                return sharedStrings[idx];
            }

            return raw;
        }
    }

    // ?? ExcelRow ?????????????????????????????????????????????????????????????

    /// <summary>
    /// Jeden wiersz danych z arkusza Excel.
    /// Wartoœci dostêpne s¹ po nazwie nag³ówka (bez uwzglêdnienia wielkoœci liter).
    /// </summary>
    public sealed class ExcelRow
    {
        private readonly Dictionary<string, string?> _values;

        /// <summary>Numer wiersza w pliku (licz¹c od 1, wiersz 1 = nag³ówek)</summary>
        public int RowNumber { get; }

        internal ExcelRow(Dictionary<string, string?> values, int rowNumber)
        {
            _values = values;
            RowNumber = rowNumber;
        }

        /// <summary>
        /// Zwraca wartoœæ komórki jako string lub null gdy komórka jest pusta.
        /// </summary>
        public string? this[string columnName] => _values.GetValueOrDefault(columnName);

        /// <summary>Zwraca wartoœæ lub pusty string gdy null.</summary>
        public string GetString(string columnName) => _values.GetValueOrDefault(columnName) ?? string.Empty;

        /// <summary>Zwraca int? lub null gdy pusta / nieparsowalna.</summary>
        public int? GetInt(string columnName)
            => int.TryParse(GetString(columnName), out var v) ? v : null;

        /// <summary>Zwraca decimal? lub null gdy pusta / nieparsowalna.</summary>
        public decimal? GetDecimal(string columnName)
            => decimal.TryParse(GetString(columnName),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v) ? v : null;

        /// <summary>Zwraca bool? — rozpoznaje: true/false, 1/0, tak/nie, yes/no, t/n.</summary>
        public bool? GetBool(string columnName)
        {
            var v = GetString(columnName).ToLowerInvariant().Trim();
            return v switch
            {
                "true" or "1" or "tak" or "yes" or "t" => true,
                "false" or "0" or "nie" or "no" or "n" => false,
                _ => null
            };
        }

        /// <summary>Zwraca DateTime? lub null gdy pusta / nieparsowalna.</summary>
        public DateTime? GetDateTime(string columnName)
            => DateTime.TryParse(GetString(columnName), out var v) ? v : null;

        /// <summary>Sprawdza czy kolumna jest niepusta.</summary>
        public bool HasValue(string columnName)
            => !string.IsNullOrWhiteSpace(_values.GetValueOrDefault(columnName));

        /// <summary>Lista wszystkich nazw nag³ówków dostêpnych w tym wierszu.</summary>
        public IReadOnlyCollection<string> Columns => _values.Keys;
    }
}
