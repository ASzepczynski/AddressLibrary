// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.
using AddressLibrary.Models;


namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Serwis do zarządzania tytułami (wojskowe, religijne, naukowe)
    /// Dane pobierane ze słownika TytulyStopnie z bazy danych
    /// </summary>
    public static class TitleManager
    {
        // ✅ ZMIANA: Usunięto tablicę titles_pl - teraz używamy słownika z bazy danych
        // Cached słownik tytułów (Skrot -> TytulStopien)
        private static Dictionary<string, TytulStopien>? _titleMap;

        // Cached HashSet dla szybkiego sprawdzania
        private static HashSet<string>? _titlesSet;

        /// <summary>
        /// Inicjalizuje słownik tytułów z bazy danych
        /// MUSI być wywołane przed użyciem innych metod!
        /// </summary>
        public static void Initialize(IEnumerable<TytulStopien> tytulyStopnie)
        {
            _titleMap = new Dictionary<string, TytulStopien>(StringComparer.OrdinalIgnoreCase);
            _titlesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tytul in tytulyStopnie)
            {
                // ✅ Dodaj wszystkie warianty ZNORMALIZOWANE (bez polskich znaków)

                // Dodaj dopełniacz (np. "świętego" → "swietego")
                DodajElement(tytul.Dopelniacz, tytul);

                // Dodaj skrót (np. "św." → "sw")
                DodajElement(tytul.Skrot, tytul);

                if (!tytul.Skrot.EndsWith(".") && !tytul.Skrot.Contains(" "))
                {
                    // Dla jednowyrazowych skrótów nie kończących się kropką
                    // Dodaj nieprawidłowy skrót (np. "mjr" → "mjr.")
                    DodajElement(tytul.Skrot + ".", tytul);
                    // Dodaj nieprawidłowy skrót (np. "bp" → "bpa")
                    DodajElement(tytul.Skrot + "a", tytul);
                }
            }
        }
        public static void DodajElement(string stopien, TytulStopien tytul)
        {
            if (!string.IsNullOrWhiteSpace(stopien))
            {
                var skrotNorm = TextNormalizer.Normalize(stopien);
                if (!_titleMap.ContainsKey(skrotNorm))
                    _titleMap[skrotNorm] = tytul;
                _titlesSet.Add(skrotNorm);
            }
        }
        /// <summary>
        /// Sprawdza czy TitleManager został zainicjalizowany
        /// </summary>
        public static bool IsInitialized => _titleMap != null && _titlesSet != null;

        /// <summary>
        /// Resetuje cache — wymusza ponowną inicjalizację przy następnym wywołaniu Initialize.
        /// </summary>
        public static void Reset()
        {
            _titleMap = null;
            _titlesSet = null;
        }

        /// <summary>
        /// Usuwa tytuły wojskowe, religijne, naukowe z tekstu (case-insensitive, bez polskich znaków)
        /// </summary>
        public static string RemoveTitles(string text)
        {
            if (string.IsNullOrEmpty(text) || _titlesSet == null)
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var filtered = words.Where(w =>
            {
                var normalized = UliceUtils.RemoveDiacritics(w.ToLowerInvariant());
                return !_titlesSet.Contains(normalized);
            }).ToList();

            return string.Join(" ", filtered);
        }

        /// <summary>
        /// Wyodrębnia tytuły z nazwy ulicy
        /// Przykład: "prof. dr mgr inż. Andrzej Szepczyński" → ("prof. dr mgr inż.", "Andrzej Szepczyński")
        /// </summary>
        /// <param name="streetName">Pełna nazwa ulicy z tytułami</param>
        /// <returns>Tuple (tytuły, nazwa bez tytułów)</returns>
        public static (string titles, string nameWithoutTitles) SplitInitialTitle(string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName) || _titlesSet == null)
                return (string.Empty, streetName ?? string.Empty);

            var words = streetName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var extractedTitles = new List<string>();
            var currentIndex = 0;

            // Iteruj po słowach i zbieraj tytuły z początku
            while (currentIndex < words.Length)
            {
                var word = words[currentIndex];

                // Normalizuj słowo (usuń kropkę, polskie znaki, lowercase)
                var normalizedWord = UliceUtils.RemoveDiacritics(word.ToLowerInvariant());

                // Sprawdź czy to tytuł
                if (_titlesSet.Contains(normalizedWord) || _titlesSet.Contains(word))
                {
                    // Dodaj oryginalne słowo (z kropką jeśli była)
                    extractedTitles.Add(word);
                    currentIndex++;
                }
                else
                {
                    // Napotkano słowo, które nie jest tytułem - przerywamy
                    break;
                }
            }

            // Złóż wynik
            var titlesString = string.Join(" ", extractedTitles);
            var remainingWords = words.Skip(currentIndex).ToArray();
            var nameWithoutTitles = string.Join(" ", remainingWords);

            return (titlesString, nameWithoutTitles);
        }

     

        /// <summary>
        /// Pobiera pełną nazwę tytułu na podstawie skrótu
        /// Przykład: "płk." → "pułkownika", "gen." → "generała"
        /// </summary>
        public static string GetTitleField(string titleOrAbbreviation, string sType)
        {
            if (string.IsNullOrWhiteSpace(titleOrAbbreviation) || _titleMap == null)
                return "";

            var normalized = UliceUtils.RemoveDiacritics(titleOrAbbreviation.ToLowerInvariant());

            if (_titleMap.TryGetValue(normalized, out var titleDef) ||
                _titleMap.TryGetValue(titleOrAbbreviation, out titleDef))
            {
                switch (sType)
                {
                    case "N": return titleDef.Nazwa;
                    case "D": return titleDef.Dopelniacz;
                    case "S": return titleDef.Skrot;
                    default: throw new Exception("Nieznane pole w stopniach/tytułach");
                }
            }

            return "";
        }



        /// <summary>
        /// Pobiera pełną nazwę tytułu na podstawie skrótu
        /// Przykład: "płk." → "pułkownika", "gen." → "generała"
        /// </summary>
        public static string GetFullName(string stopien)
        {
            return GetTitleField(stopien,"N");
        }

        /// <summary>
        /// Pobiera skrót tytułu na podstawie pełnej nazwy
        /// Przykład: "pułkownika" → "płk.", "generała" → "gen."
        /// </summary>
        public static string GetAbbreviation(string stopien)
        {
            return GetTitleField(stopien, "S");
        }

        /// <summary>
        /// Pobiera dopełniacz tytułu na podstawie skrótu lub nazwy
        /// Przykład: "płk." → "pułkownika", "generał" → "generała"
        /// </summary>
        public static string GetDopelniacz(string stopien)
        {
            return GetTitleField(stopien, "D");
        }

        /// <summary>
        /// Sprawdza czy dwa tytuły są tym samym tytułem
        /// Obsługuje różne formy: skróty (mjr., płk.), dopełniacze (majora, pułkownika), z kropkami i bez
        /// </summary>
        /// <param name="tytul1">Pierwszy tytuł do porównania</param>
        /// <param name="tytul2">Drugi tytuł do porównania</param>
        /// <returns>True jeśli tytuły są tym samym tytułem (ignorując formę i kropki)</returns>
        public static bool TenSamTytul(string? tytul1, string? tytul2)
        {
            // Oba puste lub null → równe
            if (string.IsNullOrWhiteSpace(tytul1) && string.IsNullOrWhiteSpace(tytul2))
                return true;

            // Jeden pusty, drugi nie → różne
            if (string.IsNullOrWhiteSpace(tytul1) || string.IsNullOrWhiteSpace(tytul2))
                return false;

            // Normalizuj (usuń kropki, lowercase, bez diakrytyków)
            var normalized1 = TextNormalizer.Normalize(tytul1.Trim());
            var normalized2 = TextNormalizer.Normalize(tytul2.Trim());

            // Dokładne dopasowanie
            if (normalized1 == normalized2)
                return true;

            if (!IsInitialized || _titleMap == null)
                return false;

            // Znajdź definicje tytułów w słowniku
            TytulStopien? title1 = null;
            TytulStopien? title2 = null;

            _titleMap.TryGetValue(normalized1, out title1);
            _titleMap.TryGetValue(normalized2, out title2);

            // Jeśli oba znalezione w słowniku → porównaj ich skróty (a nie ID!)
            if (title1 != null && title2 != null)
            {
                // Porównaj znormalizowane skróty (np. "św." dla obu form)
                var skrot1 = TextNormalizer.Normalize(title1.Skrot ?? "");
                var skrot2 = TextNormalizer.Normalize(title2.Skrot ?? "");

                if (!string.IsNullOrEmpty(skrot1) && !string.IsNullOrEmpty(skrot2))
                {
                    return skrot1 == skrot2;
                }

                // Fallback: porównaj ID jeśli skróty są puste
                return title1.Id == title2.Id;
            }

            // Jeśli tylko jeden znaleziony → porównaj ze wszystkimi wariantami drugiego
            if (title1 != null)
            {
                var norm1Nazwa = TextNormalizer.Normalize(title1.Nazwa ?? "");
                var norm1Dopelniacz = TextNormalizer.Normalize(title1.Dopelniacz ?? "");
                var norm1Skrot = TextNormalizer.Normalize(title1.Skrot ?? "");

                return norm1Nazwa == normalized2 ||
                       norm1Dopelniacz == normalized2 ||
                       norm1Skrot == normalized2;
            }

            if (title2 != null)
            {
                var norm2Nazwa = TextNormalizer.Normalize(title2.Nazwa ?? "");
                var norm2Dopelniacz = TextNormalizer.Normalize(title2.Dopelniacz ?? "");
                var norm2Skrot = TextNormalizer.Normalize(title2.Skrot ?? "");

                return norm2Nazwa == normalized1 ||
                       norm2Dopelniacz == normalized1 ||
                       norm2Skrot == normalized1;
            }

            // Żaden nie znaleziony → różne
            return false;
        }
    }

}
