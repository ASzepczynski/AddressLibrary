// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.
using AddressLibrary.Models;
using AddressLibrary.Utils;

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
                // Dodaj dopełniacz
                if (!string.IsNullOrWhiteSpace(tytul.Dopelniacz))
                {
                    var dopelniacz = tytul.Dopelniacz.Trim();
                    if (!_titleMap.ContainsKey(dopelniacz))
                        _titleMap[dopelniacz] = tytul;
                    _titlesSet.Add(dopelniacz);

                }
            }
        }

        /// <summary>
        /// Sprawdza czy TitleManager został zainicjalizowany
        /// </summary>
        public static bool IsInitialized => _titleMap != null && _titlesSet != null;

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
                var normalized = UliceUtils.RemoveDiacritics(w.Replace(".", "").ToLowerInvariant());
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
                var normalizedWord = UliceUtils.RemoveDiacritics(word.Replace(".", "").ToLowerInvariant());

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
        /// Normalizuje tytuły - zamienia pełne formy na skróty
        /// Przykład: "doktora profesora" → "dr. prof."
        /// </summary>
        /// <param name="titles">Ciąg tytułów do znormalizowania</param>
        /// <returns>Znormalizowany ciąg tytułów ze skrótami</returns>
        public static string NormalizeTitles(string titles)
        {
            if (string.IsNullOrWhiteSpace(titles) || _titleMap == null)
                return string.Empty;

            var words = titles.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedTitles = new List<string>();

            foreach (var word in words)
            {
                // Normalizuj słowo (usuń kropkę, polskie znaki, lowercase)
                var normalizedWord = UliceUtils.RemoveDiacritics(word.Replace(".", "").ToLowerInvariant());

                // Szukaj w słowniku
                TytulStopien? foundTitle = null;

                // Spróbuj znaleźć bezpośrednio
                if (_titleMap.TryGetValue(normalizedWord, out foundTitle) ||
                    _titleMap.TryGetValue(word, out foundTitle))
                {
                    // Dodaj skrót (jeśli jeszcze go nie ma w liście)
                    if (!string.IsNullOrWhiteSpace(foundTitle.Skrot) && !normalizedTitles.Contains(foundTitle.Skrot))
                    {
                        normalizedTitles.Add(foundTitle.Skrot);
                    }
                }
                else
                {
                    // Jeśli nie znaleziono, zachowaj oryginalne słowo
                    normalizedTitles.Add(word);
                }
            }

            return string.Join(" ", normalizedTitles);
        }

        /// <summary>
        /// Pobiera pełną nazwę tytułu na podstawie skrótu
        /// Przykład: "płk." → "pułkownika", "gen." → "generała"
        /// </summary>
        public static string? GetFullName(string titleOrAbbreviation)
        {
            if (string.IsNullOrWhiteSpace(titleOrAbbreviation) || _titleMap == null)
                return null;

            var normalized = UliceUtils.RemoveDiacritics(titleOrAbbreviation.Replace(".", "").ToLowerInvariant());

            if (_titleMap.TryGetValue(normalized, out var titleDef) ||
                _titleMap.TryGetValue(titleOrAbbreviation, out titleDef))
            {
                return titleDef.Nazwa;
            }

            return null;
        }

        /// <summary>
        /// Pobiera skrót tytułu na podstawie pełnej nazwy
        /// Przykład: "pułkownika" → "płk.", "generała" → "gen."
        /// </summary>
        public static string? GetAbbreviation(string titleOrFullName)
        {
            if (string.IsNullOrWhiteSpace(titleOrFullName) || _titleMap == null)
                return null;

            var normalized = UliceUtils.RemoveDiacritics(titleOrFullName.Replace(".", "").ToLowerInvariant());

            if (_titleMap.TryGetValue(normalized, out var titleDef) ||
                _titleMap.TryGetValue(titleOrFullName, out titleDef))
            {
                return titleDef.Skrot;
            }

            return null;
        }

        /// <summary>
        /// Pobiera dopełniacz tytułu na podstawie skrótu lub nazwy
        /// Przykład: "płk." → "pułkownika", "generał" → "generała"
        /// </summary>
        public static string? GetDopelniacz(string titleOrAbbreviation)
        {
            if (string.IsNullOrWhiteSpace(titleOrAbbreviation) || _titleMap == null)
                return null;

            var normalized = UliceUtils.RemoveDiacritics(titleOrAbbreviation.Replace(".", "").ToLowerInvariant());

            if (_titleMap.TryGetValue(normalized, out var titleDef) ||
                _titleMap.TryGetValue(titleOrAbbreviation, out titleDef))
            {
                return titleDef.Dopelniacz;
            }

            return null;
        }
    }

    /// <summary>
    /// Definicja tytułu - NIE UŻYWANE, zachowane dla kompatybilności wstecznej
    /// </summary>
    [Obsolete("Używaj modelu TytulStopien z bazy danych")]
    internal class TitleDefinition
    {
        public string Skrot { get; }
        public string Nazwa { get; }
        public string[] Synonimy { get; }

        public TitleDefinition(string skrot, string nazwa, params string[] synonimy)
        {
            Skrot = skrot;
            Nazwa = nazwa;
            Synonimy = synonimy;
        }
    }
}
