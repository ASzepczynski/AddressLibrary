using AddressLibrary.Logging;
using System.Text.RegularExpressions;
using AddressLibrary.Models;

namespace AddressLibrary.Helpers
{
    public static class TerytUlicPoprawkiDictionary
    {
        /// <summary>
        /// Wczytuje s³ownik TerytUlicPoprawki z pliku Excel.
        /// Oczekiwane nag³ówki: Cecha | Prefiks | Tytul | Imie | Imie2 | Nazwisko | Nazwisko2 | Pseudonim | Postfiks | Id
        /// </summary>
        public static Dictionary<string, TerytUlicPoprawka> Load(string appDataPath, GeneralLogger logger)
        {
            var dictionary = new Dictionary<string, TerytUlicPoprawka>(StringComparer.OrdinalIgnoreCase);
            var excelPath = Path.Combine(appDataPath, "AppData", "Dictionaries", "TerytUlicPoprawki.xlsx");

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"?? Plik {excelPath} nie istnieje");
                logger.LogError($"Plik s³ownika nie istnieje: {excelPath}");
                return dictionary;
            }

            try
            {
                var rows = ExcelTableReader.Read(excelPath);

                foreach (var row in rows)
                {
                    var id = row["Id"]?.Trim();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    {
                        var cechaVal = row.GetString("Cecha");
                        var prefiksVal = row.GetString("Prefiks");

                        // Usuñ wyst¹pienia s³owa "im." z prefiksu (ignoruj wielkoœæ liter)
                        if (!string.IsNullOrWhiteSpace(prefiksVal))
                        {
                            var tokens = prefiksVal
                                .Split((new char[] { ' ' }), StringSplitOptions.RemoveEmptyEntries)
                                .Where(t => !string.Equals(t, "im.", StringComparison.OrdinalIgnoreCase))
                                .ToArray();

                            prefiksVal = tokens.Length == 0 ? "" : string.Join(" ", tokens);
                        }
                        var tytulVal = row.GetString("Tytul");
                        var imieVal = row.GetString("Imie");
                        var imie2Val = row.GetString("Imie2");
                        var nazwiskoVal = row.GetString("Nazwisko");
                        var nazwisko2Val = row.GetString("Nazwisko2");
                        var pseudonimVal = row.GetString("Pseudonim");
                        var postfiksVal = row.GetString("Postfiks");

                        // Je¿eli wpis ma tylko cechê (np. "rynek") a pozosta³e pola s¹ puste,
                        // to zachowujemy dotychczasowe zachowanie: ustawiamy Cecha = "inne"
                        // a oryginaln¹ nazwê cechy zapisujemy w Postfiks (z wielk¹ liter¹).
                        if (!string.IsNullOrWhiteSpace(cechaVal)
                            && string.IsNullOrWhiteSpace(prefiksVal)
                            && string.IsNullOrWhiteSpace(tytulVal)
                            && string.IsNullOrWhiteSpace(imieVal)
                            && string.IsNullOrWhiteSpace(imie2Val)
                            && string.IsNullOrWhiteSpace(nazwiskoVal)
                            && string.IsNullOrWhiteSpace(nazwisko2Val)
                            && string.IsNullOrWhiteSpace(pseudonimVal)
                            && string.IsNullOrWhiteSpace(postfiksVal))
                        {
                            // Przenieœ oryginaln¹ cechê do Postfiks i ustaw Cecha na "inne"
                            var txt = cechaVal.Trim();
                            var post = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(txt.ToLower());
                            dictionary[id] = new TerytUlicPoprawka
                            {
                                TerytId = id,
                                Cecha = "inne",
                                Prefiks = null,
                                Tytul = null,
                                Imie = null,
                                Imie2 = null,
                                Nazwisko = null,
                                Nazwisko2 = null,
                                Pseudonim = null,
                                Postfiks = post
                            };
                        }
                        else
                        {
                            dictionary[id] = new TerytUlicPoprawka
                            {
                                TerytId = id,
                                Cecha = cechaVal,
                                Prefiks = prefiksVal,
                                Tytul = tytulVal,
                                Imie = imieVal,
                                Imie2 = imie2Val,
                                Nazwisko = nazwiskoVal,
                                Nazwisko2 = nazwisko2Val,
                                Pseudonim = pseudonimVal,
                                Postfiks = postfiksVal
                            };
                        }
                    }
                }

                Console.WriteLine($"? Za³adowano {dictionary.Count} wpisów ze s³ownika TerytUlicPoprawki.xlsx");
                logger.LogInfo($"Za³adowano {dictionary.Count} wpisów ze s³ownika");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? B³¹d ³adowania s³ownika TerytUlicPoprawka: {ex.Message}");
                logger.LogError($"B³¹d ³adowania s³ownika: {ex.Message}");
            }

            return dictionary;
        }
    }
}
