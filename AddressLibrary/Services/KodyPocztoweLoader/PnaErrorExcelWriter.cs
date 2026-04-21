using AddressLibrary.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AddressLibrary.Services.KodyPocztoweLoader
{
    /// <summary>
    /// Zbiera b³êdne rekordy PNA podczas ³adowania i zapisuje je do pliku Excel.
    /// Ka¿dy b³¹d zapisywany jest jako dwa wiersze (orygina³ + pusty wiersz do korekty).
    /// Kolumny: Kod | Miasto | Ulica | Numery | Gmina | Powiat | Wojewodztwo | Komentarz
    /// </summary>
    public class PnaErrorExcelWriter
    {
        private readonly List<(Pna Pna, string Komentarz)> _errors = new();

        public int Count => _errors.Count;

        public void Add(Pna pna, string komentarz)
        {
            _errors.Add((pna, komentarz));
        }

        public void Save(string appDataPath)
        {
            if (_errors.Count == 0)
                return;

            var dir = Path.Combine(appDataPath, "AppData", "pna");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "BledyPnaPropozycje.xlsx");

            using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);

            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id    = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name  = "B³êdy PNA"
            });

            // Nag³ówek
            sheetData.Append(MakeRow(0,
                "Kod", "Miasto", "Dzielnica","Ulica", "Numery",
                "Gmina", "Powiat", "Województwo", "Komentarz"));

            uint rowIndex = 1;
            foreach (var (pna, komentarz) in _errors)
            {
                // Wiersz oryginalny (z komentarzem b³êdu)
                sheetData.Append(MakeRow(rowIndex++,
                    pna.Kod, pna.Miasto, pna.Dzielnica, pna.Ulica, pna.Numery,
                    pna.Gmina, pna.Powiat, pna.Wojewodztwo, komentarz));

                // Wiersz do korekty (wszystkie pola identyczne, komentarz pusty)
                sheetData.Append(MakeRow(rowIndex++,
                    pna.Kod, pna.Miasto, pna.Dzielnica, pna.Ulica, pna.Numery,
                    pna.Gmina, pna.Powiat, pna.Wojewodztwo, string.Empty));
            }

            workbookPart.Workbook.Save();
        }

        private static Row MakeRow(uint rowIndex, params string[] values)
        {
            var row = new Row { RowIndex = rowIndex + 1 };
            for (int i = 0; i < values.Length; i++)
            {
                row.Append(new Cell
                {
                    CellReference = $"{ColLetter(i)}{rowIndex + 1}",
                    DataType      = CellValues.InlineString,
                    InlineString  = new InlineString(new Text(values[i] ?? string.Empty))
                });
            }
            return row;
        }

        private static string ColLetter(int index)
        {
            // A-Z, potem AA, AB... (wystarczy dla 8 kolumn)
            if (index < 26) return ((char)('A' + index)).ToString();
            return ((char)('A' + index / 26 - 1)).ToString() +
                   ((char)('A' + index % 26)).ToString();
        }
    }
}
