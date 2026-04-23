using AddressLibrary.Models;

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
                return;

            var rows = ExcelTableReader.Read(excelPath);

            // Przetwarzaj pary wierszy (stary rekord + nowy rekord)
            for (int i = 0; i + 1 < rows.Count; i += 2)
            {
                var oldPna = ParsePnaFromRow(rows[i], out var comment);
                var newPna = ParsePnaFromRow(rows[i + 1], out _);

                if (oldPna != null && newPna != null)
                {
                    _corrections.Add(new PnaCorrectionPair
                    {
                        OldPna  = oldPna,
                        NewPna  = newPna,
                        Comment = comment
                    });
                }
            }
        }

        /// <summary>
        /// Parsuje rekord PNA z wiersza ExcelRow
        /// </summary>
        private static PnaWithComment? ParsePnaFromRow(ExcelRow row, out string comment)
        {
            comment = string.Empty;
            try
            {
                var pna = new PnaWithComment
                {
                    Kod          = row.GetString("Kod"),
                    Miasto       = row.GetString("Miasto"),
                    Dzielnica    = row.GetString("Dzielnica"),
                    Ulica        = row.GetString("Ulica"),
                    Numery       = row.GetString("Numery"),
                    Gmina        = row.GetString("Gmina"),
                    Powiat       = row.GetString("Powiat"),
                    Wojewodztwo  = row.GetString("Województwo")
                };
                comment      = row.GetString("Komentarz");
                pna.Comment  = comment;
                return pna;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Próbuje znaleźć korektę dla podanego rekordu PNA
        /// </summary>
        /// <param name="pna">Rekord PNA do sprawdzenia</param>
        /// <returns>Skorygowany rekord PNA jeśli znaleziono dopasowanie, null w przeciwnym razie</returns>
        public Pna? TryCorrect(Pna pna)
        {
            if (pna == null)
                return null;

            foreach (var correction in _corrections)
            {
                if (correction.OldPna.Ulica.Contains("Katedralny"))
                {
                    int v = 1;
                }
                if (IsMatch(pna, correction.OldPna))
                {
                    // Znaleziono dopasowanie - zwróć nowy rekord
                    return new Pna
                    {
                        Kod = correction.NewPna.Kod,
                        Miasto = correction.NewPna.Miasto,
                        Dzielnica = correction.NewPna.Dzielnica,
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
                   (string.Equals(pna.Dzielnica, pattern.Dzielnica, StringComparison.OrdinalIgnoreCase)
                   ||
// Żeby uniknąć dzielnic typu Praga-Północ
                   pattern.Dzielnica=="")
                   &&
                   string.Equals(pna.Ulica, pattern.Ulica, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Numery, pattern.Numery, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Gmina, pattern.Gmina, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Powiat, pattern.Powiat, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pna.Wojewodztwo, pattern.Wojewodztwo, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Zwraca liczbę załadowanych korekt
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