// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Services.AddressSearch;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do dopasowywania nazw ulic (zoptymalizowany - używa cache)
    /// </summary>
    public class StreetMatcher
    {
        private readonly TextNormalizer _normalizer;

        public StreetMatcher(TextNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        /// <summary>
        /// 🚀 ZOPTYMALIZOWANA: Sprawdza czy ulica pasuje (używa pre-znormalizowanych nazw)
        /// Tylko dokładne dopasowanie (equality) - BEZ partial match
        /// NIGDY nie porównuj z samą Nazwa2!
        /// </summary>
        public bool IsMatch(UlicaCached ulica, string normalizedSearchTerm)
        {
            // ✅ Sprawdź dokładne dopasowanie głównej nazwy
            if (ulica.NormalizedNazwa1 == normalizedSearchTerm)
                return true;

            // ✅ Sprawdź dokładne dopasowanie kombinacji
            if (ulica.NormalizedCombined != null && ulica.NormalizedCombined == normalizedSearchTerm)
                return true;

            // ✅ NOWE: Sprawdź dopasowanie po nazwisku (końcówka NormalizedNazwa1)
            // Obsługuje przypadki: "Axentowicza" → "teodora axentowicza"
            if (ulica.NormalizedNazwa1.EndsWith(" " + normalizedSearchTerm))
                return true;

            // ✅ NOWE: Sprawdź dopasowanie po nazwisku (końcówka NormalizedCombined)
            if (ulica.NormalizedCombined != null && 
                ulica.NormalizedCombined.EndsWith(" " + normalizedSearchTerm))
                return true;

            return false;
        }

        /// <summary>
        /// 🆕 Znajduje ulicę metodą HIERARCHICZNĄ:
        /// 1. Dokładne dopasowanie (equality)
        /// 2. Retry bez skrótu imienia (G.Zapolskiej -> Zapolskiej)
        /// 3. Dopasowanie częściowe (contains) jako ostateczny fallback
        /// </summary>
        public UlicaCached? FindStreet(List<UlicaCached> ulice, string originalStreetName)
        {
            // ✅ KROK 1: Dokładne dopasowanie z oryginalną nazwą
            var normalized = _normalizer.Normalize(originalStreetName);

            foreach (var ulica in ulice)
            {
                if (IsMatch(ulica, normalized))
                    return ulica;
            }

            // ✅ KROK 2: Retry bez skrótu imienia (G.Zapolskiej -> Zapolskiej)
            var withoutInitial = _normalizer.RemoveNameInitial(originalStreetName);

            if (withoutInitial != originalStreetName)
            {
                var normalizedWithoutInitial = _normalizer.Normalize(withoutInitial);

                foreach (var ulica in ulice)
                {
                    if (IsMatch(ulica, normalizedWithoutInitial))
                        return ulica;
                }
            }

            // ⚠️ KROK 3: Dopasowanie częściowe (TYLKO gdy nie znaleziono dokładnego)
            foreach (var ulica in ulice)
            {
                // ✅ Sprawdź Nazwa1
                if (IsPartialMatch(ulica.NormalizedNazwa1, normalized))
                    return ulica;

                // ✅ Sprawdź pełne kombinacje (NormalizedCombined i NormalizedCombinedReverse)
                if (ulica.NormalizedCombined != null && IsPartialMatch(ulica.NormalizedCombined, normalized))
                    return ulica;

            }

            return null;
        }

        /// <summary>
        /// 🆕 Znajduje WSZYSTKIE ulice pasujące do wyszukiwanej nazwy (hierarchicznie)
        /// </summary>
        public List<UlicaCached> FindAllStreets(List<UlicaCached> ulice, string originalStreetName)
        {
            var results = new List<UlicaCached>();
            var normalized = _normalizer.Normalize(originalStreetName);

            // ✅ KROK 1: Dokładne dopasowanie z oryginalną nazwą
            foreach (var ulica in ulice)
            {
                if (IsMatch(ulica, normalized))
                    results.Add(ulica);
            }

            // Jeśli znaleziono dokładne dopasowania, zwróć je
            if (results.Count > 0)
                return results;

            // ✅ KROK 2: Retry bez skrótu imienia (G.Zapolskiej -> Zapolskiej)
            var withoutInitial = _normalizer.RemoveNameInitial(originalStreetName);

            if (withoutInitial != originalStreetName)
            {
                var normalizedWithoutInitial = _normalizer.Normalize(withoutInitial);

                foreach (var ulica in ulice)
                {
                    if (IsMatch(ulica, normalizedWithoutInitial))
                        results.Add(ulica);
                }
            }

            // Jeśli znaleziono po usunięciu inicjału, zwróć je
            if (results.Count > 0)
                return results;

            // ⚠️ KROK 3: Dopasowanie częściowe (TYLKO gdy nie znaleziono dokładnego)
            foreach (var ulica in ulice)
            {
                // ✅ Sprawdź Nazwa1
                bool matchesNazwa1 = IsPartialMatch(ulica.NormalizedNazwa1, normalized);

                // ✅ Sprawdź pełne kombinacje
                bool matchesCombined = ulica.NormalizedCombined != null &&
                                      IsPartialMatch(ulica.NormalizedCombined, normalized);

                              if (matchesNazwa1 || matchesCombined )
                {
                    results.Add(ulica);
                }
            }

            return results;
        }

        /// <summary>
        /// 🆕 Znajduje ulicę TYLKO metodą dokładnego dopasowania (bez partial match)
        /// Używane gdy priorytetem jest precyzja (np. "Powstańców" nie może znaleźć "Powstańców Śląskich")
        /// </summary>
        public UlicaCached? FindStreetExact(List<UlicaCached> ulice, string searchName)
        {
            var normalizedSearch = _normalizer.Normalize(searchName);

            foreach (var ulica in ulice)
            {
                // ✅ Sprawdź TYLKO:
                // 1. Nazwa1
                // 2. Nazwa2 + Nazwa1 (NormalizedCombined)
                // 3. Nazwa1 + Nazwa2 (NormalizedCombinedReverse)

                if (ulica.NormalizedNazwa1 == normalizedSearch ||
                    ulica.NormalizedCombined == normalizedSearch )
                {
                    return ulica;
                }
            }

            return null;
        }

        /// <summary>
        /// Dopasowanie częściowe - sprawdza czy searchTerm jest CAŁYM SŁOWEM lub OSTATNIM SŁOWEM w nazwie ulicy
        /// Obsługuje nazwy patronów (np. "Łokietka" znajdzie "Władysława Łokietka")
        /// </summary>
        private bool IsPartialMatch(string normalizedStreetName, string searchTerm)
        {
            // Split tylko raz, bez dodatkowej normalizacji
            var words = normalizedStreetName.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);

            // ✅ KROK 1: Sprawdź dokładne dopasowanie do któregokolwiek słowa
            if (Array.IndexOf(words, searchTerm) >= 0)
                return true;

            // ✅ KROK 2: Sprawdź czy nazwa ulicy kończy się na " " + searchTerm (dla patronów)
            // Przykład: "wladyslawa lokietka" kończy się na " lokietka" ✅
            //          "lowiecka" NIE kończy się na " lokietka" ❌
            if (normalizedStreetName.EndsWith(" " + searchTerm))
                return true;

            return false;
        }

        /// <summary>
        /// Znajduje najbardziej podobną ulicę (fuzzy matching) używając odległości Levenshteina
        /// </summary>
        public UlicaCached? FindMostSimilarStreet(List<UlicaCached> ulice, string searchTerm, int maxDistance = 2)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return null;

            var normalizedSearch = _normalizer.Normalize(searchTerm);

            UlicaCached? bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var ulica in ulice)
            {
                int distance1 = LevenshteinDistance(normalizedSearch, ulica.NormalizedNazwa1);

                int distanceCombined = int.MaxValue;
                if (!string.IsNullOrEmpty(ulica.NormalizedCombined))
                {
                    distanceCombined = LevenshteinDistance(normalizedSearch, ulica.NormalizedCombined);
                }

                int minDistance = Math.Min(distance1, distanceCombined);

                if (minDistance < bestDistance)
                {
                    bestDistance = minDistance;
                    bestMatch = ulica;
                }
            }

            if (bestMatch != null)
            {
                var referenceLength = Math.Max(normalizedSearch.Length, bestMatch.NormalizedNazwa1.Length);
                var similarity = 1.0 - ((double)bestDistance / referenceLength);

                // 🔧 POPRAWKA: Wyższy próg dla krótkich słów
                double minSimilarity = normalizedSearch.Length <= 5 ? 0.7 : 0.5;

                if (bestDistance <= maxDistance && similarity >= minSimilarity)
                    return bestMatch;
            }

            return null;
        }

        /// <summary>
        /// Oblicza odległość Levenshteina między dwoma stringami
        /// </summary>
        private int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
                return string.IsNullOrEmpty(t) ? 0 : t.Length;

            if (string.IsNullOrEmpty(t))
                return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
