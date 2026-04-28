using AddressLibrary.Helpers;
using AddressLibrary.Logging;

namespace AddressLibrary.Dictionaries.Pseudonimy
{
    /// <summary>
    /// S³ownik pseudonimów: mianownik ? dope³niacz.
    /// £adowany z pliku AppData/Dictionaries/Pseudonimy.xlsx
    /// Struktura pliku: nag³ówek w wierszu 1, kolumna A = Mianownik, kolumna B = Dope³niacz
    /// </summary>
    public static class PseudonimiDictionary
    {
        /// <summary>
        /// Wczytuje s³ownik pseudonimów z pliku Excel.
        /// Zwraca Dictionary&lt;mianownik, dope³niacz&gt; (porównywanie ignoruje wielkoœæ liter).
        /// Jeœli plik nie istnieje, zwraca pusty s³ownik.
        /// </summary>
        public static Dictionary<string, string> Load(string appDataPath, PostalCodesLogger? logger = null)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var excelPath = Path.Combine(appDataPath, "AppData", "Dictionaries", "Pseudonimy.xlsx");

            if (!File.Exists(excelPath))
            {
                logger?.LogWarning($"[PseudonimiDictionary] Plik nie istnieje: {excelPath}");
                return result;
            }

            try
            {
                var rows = ExcelTableReader.Read(excelPath);

                foreach (var row in rows)
                {
                    var mianownik  = row.GetString("Mianownik").Trim();
                    var dopelniacz = row.GetString("Dope³niacz").Trim();

                    if (string.IsNullOrWhiteSpace(mianownik))
                        continue;

                    result[mianownik] = dopelniacz;
                }

                logger?.LogInfo($"[PseudonimiDictionary] Za³adowano {result.Count} pseudonimów z: {excelPath}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[PseudonimiDictionary] B³¹d wczytywania: {ex.Message}");
            }

            return result;
        }
    }
}
