using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using AddressLibrary.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Helper do wczytywania i stosowania korekt danych PNA z pliku Excel (AppData/Pna/KorektyPna.xlsx)
    /// Format: pary linii - pierwsza to stary rekord, druga to nowy rekord + komentarz
    /// </summary>
    public class PnaCorrectionHelper
    {
        private readonly List<PnaCorrectionPair> _corrections;

        public PnaCorrectionHelper(string appDataPath)
        {
            _corrections = new List<PnaCorrectionPair>();
            LoadFromExcel(appDataPath);
        }

        /// <summary>
        /// Wczytuje korekty z pliku Excel
        /// </summary>
        private void LoadFromExcel(string appDataPath)
        {
            var excelPath = Path.Combine(appDataPath, "AppData", "Pna", "KorektyPna.xlsx");

            if (!File.Exists(excelPath))
            {
                // Plik nie istnieje - helper bêdzie pusty (bezpieczne)
                return;
            }

            using var document = SpreadsheetDocument.Open(excelPath, false);
            var workbookPart = document.WorkbookPart;
            var worksheetPart = workbookPart?.WorksheetParts.First();
            var sheetData = worksheetPart?.Worksheet.Elements<SheetData>().First();

            if (sheetData == null)
                return;

            var rows = sheetData.Elements<Row>().Skip(1).ToList(); // Pomiñ nag³ówek

            // Przetwarzaj pary wierszy (stary rekord + nowy rekord)
            for (int i = 0; i < rows.Count - 1; i += 2)
            {
                var oldRow = rows[i];
                var newRow = rows[i + 1];

                var oldPna = ParsePnaFromRow(workbookPart, oldRow, out _);
                var newPna = ParsePnaFromRow(workbookPart, newRow, out var comment);

                if (oldPna != null && newPna != null)
                {
                    _corrections.Add(new PnaCorrectionPair
                    {
                        OldPna = oldPna,
                        NewPna = newPna,
                        Comment = comment
                    });
                }
            }
        }

        /// <summary>
        /// Parsuje rekord PNA z wiersza Excel
        /// </summary>
        private PnaWithComment? ParsePnaFromRow(WorkbookPart? workbookPart, Row row, out string comment)
        {
            comment = string.Empty;
            var cells = row.Elements<Cell>().ToList();

            if (cells.Count < 7) // Minimum 7 kolumn (bez komentarza)
                return null;

            try
            {
                var pna = new PnaWithComment
                {
                    Kod = GetCellValue(workbookPart, cells[0]).Trim(),
                    Miasto = GetCellValue(workbookPart, cells[1]).Trim(),
                    Ulica = GetCellValue(workbookPart, cells[2]).Trim(),
                    Numery = GetCellValue(workbookPart, cells[3]).Trim(),
                    Gmina = GetCellValue(workbookPart, cells[4]).Trim(),
                    Powiat = GetCellValue(workbookPart, cells[5]).Trim(),
                    Wojewodztwo = GetCellValue(workbookPart, cells[6]).Trim()
                };

                // Opcjonalna kolumna komentarza (8. kolumna)
                if (cells.Count > 7)
                {
                    comment = GetCellValue(workbookPart, cells[7]).Trim();
                    pna.Comment = comment;
                }

                return pna;
            }
            catch
            {
                return null;
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
        /// Próbuje znaleŸæ korektê dla podanego rekordu PNA
        /// </summary>
        /// <param name="pna">Rekord PNA do sprawdzenia</param>
        /// <returns>Skorygowany rekord PNA jeœli znaleziono dopasowanie, null w przeciwnym razie</returns>
        public Pna? TryCorrect(Pna pna)
        {
            if (pna == null)
                return null;

            foreach (var correction in _corrections)
            {
                if (IsMatch(pna, correction.OldPna))
                {
                    // Znaleziono dopasowanie - zwróæ nowy rekord
                    return new Pna
                    {
                        Kod = correction.NewPna.Kod,
                        Miasto = correction.NewPna.Miasto,
                        Ulica = correction.NewPna.Ulica,
                        Numery = correction.NewPna.Numery,
                        Gmina = correction.NewPna.Gmina,
                        Powiat = correction.NewPna.Powiat,
                        Wojewodztwo = correction.NewPna.Wojewodztwo
                    };
                }
            }

            return null; // Brak korekty
        }

        /// <summary>
        /// Sprawdza czy rekord PNA pasuje do wzorca (porównuje wszystkie pola)
        /// </summary>
        private bool IsMatch(Pna pna, PnaWithComment pattern)
        {
            return string.Equals(pna.Kod, pattern.Kod, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Miasto, pattern.Miasto, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Ulica, pattern.Ulica, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Numery, pattern.Numery, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Gmina, pattern.Gmina, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Powiat, pattern.Powiat, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Wojewodztwo, pattern.Wojewodztwo, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Zwraca liczbê za³adowanych korekt
        /// </summary>
        public int Count => _corrections.Count;
    }

    /// <summary>
    /// Para korekt: stary rekord -> nowy rekord
    /// </summary>
    internal class PnaCorrectionPair
    {
        public PnaWithComment OldPna { get; set; } = null!;
        public PnaWithComment NewPna { get; set; } = null!;
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Rozszerzenie klasy Pna z polem komentarza
    /// </summary>
    internal class PnaWithComment : Pna
    {
        public string Comment { get; set; } = string.Empty;
    }
}