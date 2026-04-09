using AddressLibrary.Logging;
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

                    dictionary[id] = new TerytUlicPoprawka
                    {
                        TerytId   = id,
                        Cecha     = row.GetString("Cecha"),
                        Prefiks   = row.GetString("Prefiks"),
                        Tytul     = row.GetString("Tytul"),
                        Imie      = row.GetString("Imie"),
                        Imie2     = row.GetString("Imie2"),
                        Nazwisko  = row.GetString("Nazwisko"),
                        Nazwisko2 = row.GetString("Nazwisko2"),
                        Pseudonim = row.GetString("Pseudonim"),
                        Postfiks  = row.GetString("Postfiks")
                    };
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
