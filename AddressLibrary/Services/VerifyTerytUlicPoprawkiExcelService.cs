using AddressLibrary.Data;
using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Helpers;
using AddressLibrary.Services.AddressSearch;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Weryfikuje spójność pliku TerytUlicPoprawki.xlsx:
    /// parsuje kolumnę J (TerytId = pełna nazwa ulicy) i porównuje wynik
    /// z rozbiciem na komponenty w kolumnach A–I.
    /// Zapisuje wynikowy plik z dopiskiem _weryf zawierający kolumnę Status
    /// (pusta = OK, "Błąd" = niezgodność) oraz wszystkie kolumny źródłowe.
    /// </summary>
    public class VerifyTerytUlicPoprawkiExcelService : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;

        public VerifyTerytUlicPoprawkiExcelService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
        }

        public async Task<VerifyTerytUlicPoprawkiResult> VerifyAsync(IProgress<string>? progress = null)
        {
            var result = new VerifyTerytUlicPoprawkiResult();

            var inputPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", "TerytUlicPoprawki.xlsx");
            Console.WriteLine($"[VerifyTerytUlicPoprawki] Plik wejściowy: {inputPath}");
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"[VerifyTerytUlicPoprawki] ✗ Plik nie istnieje!");
                throw new FileNotFoundException($"Plik nie istnieje: {inputPath}");
            }

            var outputPath = Path.Combine(
                Path.GetDirectoryName(inputPath)!,
                Path.GetFileNameWithoutExtension(inputPath) + "_weryf.xlsx");

            // Inicjalizuj parser
            Console.WriteLine($"[VerifyTerytUlicPoprawki] Inicjalizacja StreetParser...");
            progress?.Report("Inicjalizacja StreetParser...");
            var parser = new StreetParser(_context);
            await parser.InitializeAsync();
            Console.WriteLine($"[VerifyTerytUlicPoprawki] ✓ StreetParser zainicjalizowany");

            // Wczytaj wiersze
            Console.WriteLine($"[VerifyTerytUlicPoprawki] Wczytywanie pliku Excel...");
            progress?.Report("Wczytywanie pliku Excel...");
            var rows = ExcelTableReader.Read(inputPath);
            result.TotalCount = rows.Count;
            Console.WriteLine($"[VerifyTerytUlicPoprawki] Wczytano {rows.Count} wierszy");

            // Kolumny nagłówkowe (oryginalne z pliku)
            var columnNames = new[] { "Cecha", "Prefiks", "Tytul", "Imie", "Imie2", "Nazwisko", "Nazwisko2", "Pseudonim", "Postfiks", "Id" };

            var outputRows = new List<(string Status, string[] Cols)>();
            const int reportInterval = 500;

            foreach (var row in rows)
            {
                var TerytCecha     = row.GetString("Cecha");
                var prefiks   = row.GetString("Prefiks");
                var TerytTytul     = row.GetString("Tytul");
                var imie      = row.GetString("Imie");
                var imie2     = row.GetString("Imie2");
                var nazwisko  = row.GetString("Nazwisko");
                var nazwisko2 = row.GetString("Nazwisko2");
                var pseudonim = row.GetString("Pseudonim");
                var postfiks  = row.GetString("Postfiks");
                var TerytId        = row.GetString("Id");

                var NoweId= PoprawTerytId(TerytId);


                var cecha = CechyUlicUtils.GetStreetAbbreviation(TerytCecha);
                var tytul = TitleManager.GetAbbreviation(TerytTytul);
                bool ok = false;

                if (tytul == "" && imie == "" && nazwisko == "" && pseudonim == "")
                {
                    // bezosobowe
                    var wzorzec = $"{cecha} {prefiks}".Trim();
                    wzorzec = $"{wzorzec} {postfiks}".Trim();
                    ok = Eq(wzorzec, NoweId); 
                }
                else {
                    var parsed = parser.Parse(NoweId);
                    ok =
                        Eq(cecha, parsed.Cecha) &&
                        Eq(prefiks, parsed.Prefiks) &&
                        Eq(tytul, parsed.Tytul) &&
                        Eq(imie, parsed.Imie) &&
                        Eq(imie2, parsed.Imie2) &&
                        Eq(nazwisko, parsed.Nazwisko) &&
                        Eq(nazwisko2, parsed.Nazwisko2) &&
                        Eq(pseudonim, parsed.Pseudonim) &&
                        Eq(postfiks, parsed.Postfiks);

                }
                    var status = ok ? "" : "Błąd";
                if (!ok)
                {
                    result.ErrorCount++;
                    Console.WriteLine($"[VerifyTerytUlicPoprawki] ✗ Błąd [{result.ProcessedCount + 1}]: '{TerytId}'");
                }

                outputRows.Add((status, new[]
                {
                    TerytCecha, prefiks, TerytTytul, imie, imie2,
                    nazwisko, nazwisko2, pseudonim, postfiks, TerytId
                }));

                result.ProcessedCount++;

                if (result.ProcessedCount % reportInterval == 0)
                    Console.WriteLine($"[VerifyTerytUlicPoprawki] Przetworzono {result.ProcessedCount}/{result.TotalCount}, błędów: {result.ErrorCount}");
            }

            Console.WriteLine($"[VerifyTerytUlicPoprawki] Przetworzono łącznie {result.ProcessedCount}/{result.TotalCount}, błędów: {result.ErrorCount}");

            // Zapisz wynik
            Console.WriteLine($"[VerifyTerytUlicPoprawki] Zapisywanie wyników do: {outputPath}");
            progress?.Report($"Zapisywanie wyników do {Path.GetFileName(outputPath)}...");
            WriteOutputExcel(outputPath, columnNames, outputRows);
            Console.WriteLine($"[VerifyTerytUlicPoprawki] ✓ Gotowe.");

            result.OutputPath = outputPath;
            return result;
        }

        private string PoprawTerytId(string id) {
            id = id.Replace("al. Aleja ", "al. ");
            id = id.Replace("al. Al.", "al.");
            id = id.Replace("al. aleja ", "al. ");
            id = id.Replace("ul. Aleja ", "al. ");
            id = id.Replace("ul. Al.", "al.");

            id = id.Replace("bulw. Bulwar ", "bulw. ");
            id = id.Replace("bulw. Bulwary ", "bulwary ");
            id = id.Replace("al. Bulwar ", "bulw. ");

            id = id.Replace("os. Os.", "os.");
            id = id.Replace("os. Osiedle ", "os. ");

            id = id.Replace("park Park ", "park ");

            id = id.Replace("pl. Plac ", "pl. ");

            id = id.Replace("rondo Rondo ", "rondo ");

            id = id.Replace("skwer Skwer ", "skw. ");

            id = id.Replace("inne Kolonia ", "kol. ");
            id = id.Replace("ul. Kolonia ", "kol. ");
            id = id.Replace("ul. Kol. ", "kol. ");

            id = id.Replace("ul. Bulwar ", "bulw. ");

            id = id.Replace("ul. Trakt ", "trakt ");

            id = id.Replace("ul. Droga ", "droga ");

            id = id.Replace("ul. Wały ", "wały ");
            id = id.Replace("ul. Wał ", "wał ");

            id = id.Replace("ul. Szosa ", "szosa ");

            id = id.Replace("ul. Rynek ", "rynek ");

            id = id.Replace("ul. Osiedle ", "os. ");

            id = id.Replace("ul. Osada ", "osada ");

            id = id.Replace("ul. Plac ", "pl. ");

            id = id.Replace("ul. Pasaż ", "pasaż ");

            id = id.Replace("ul. Promenada ", "promenada ");
            id = id.Replace("al. Promenada ", "promenada ");

            id = id.Replace("ul. Wzgórze ", "wzgórze ");

            id = id.Replace("ul. Wybudowanie ", "wybudowanie ");

            id = id.Replace("wyb. Wybrzeże ", "wyb. ");

            id = id.Replace("ul. Zaułek ", "zaułek ");

            id = id.Replace("inne Most ", "most ");
            id = id.Replace("inne Aleja ", "al. ");
            id = id.Replace("inne most ", "most ");
            id = id.Replace("inne Nabrzeże ", "nabrzeże ");
            id = id.Replace("inne Pasaż ", "pasaż ");
            id = id.Replace("inne Wiadukt ", "wiadukt ");
            id = id.Replace("inne Promenada ", "promenada ");
            id = id.Replace("inne Zaułek ", "zaułek ");
            id = id.Replace("inne Ogród ", "ogród ");


            id = id.Replace("\"","");
            return id;
        }


        /// <summary>
        /// Porównuje pole źródłowe z wartością sparsowaną (po normalizacji).
        /// </summary>
        private static bool Eq(string sourceValue, string parsedValue)
        {
            var normSource = TextNormalizer.Normalize(sourceValue);
            var normParsed = TextNormalizer.Normalize(parsedValue);
            return string.Equals(normSource, normParsed, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteOutputExcel(
            string path,
            string[] columnNames,
            List<(string Status, string[] Cols)> rows)
        {
            using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);

            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Weryfikacja"
            });

            // Nagłówek
            var headerValues = new[] { "Status" }.Concat(columnNames).ToArray();
            sheetData.Append(MakeRow(1, headerValues));

            uint rowIndex = 2;
            foreach (var (status, cols) in rows)
            {
                var values = new[] { status }.Concat(cols).ToArray();
                sheetData.Append(MakeRow(rowIndex++, values));
            }

            workbookPart.Workbook.Save();
        }

        private static Row MakeRow(uint rowIndex, params string[] values)
        {
            var row = new Row { RowIndex = rowIndex };
            for (int i = 0; i < values.Length; i++)
            {
                row.Append(new Cell
                {
                    CellReference = $"{ColLetter(i)}{rowIndex}",
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(values[i] ?? string.Empty))
                });
            }
            return row;
        }

        private static string ColLetter(int index)
        {
            if (index < 26)
                return ((char)('A' + index)).ToString();
            return ((char)('A' + index / 26 - 1)).ToString() + ((char)('A' + index % 26)).ToString();
        }

        public void Dispose() { }
    }

    public class VerifyTerytUlicPoprawkiResult
    {
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorCount { get; set; }
        public string OutputPath { get; set; } = string.Empty;
    }
}
