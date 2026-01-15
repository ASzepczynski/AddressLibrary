// Copyright (c) 2025 Andrzej Szepczyński. All rights reserved.

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
        /// </summary>
        public bool IsMatch(UlicaCached ulica, string normalizedSearchTerm)
        {
            // ✅ Dokładne dopasowania (equality only)
            
            // Sprawdź główną nazwę
            if (ulica.NormalizedNazwa1 == normalizedSearchTerm)
                return true;

            // Sprawdź alternatywną nazwę
            if (ulica.NormalizedNazwa2 != null && ulica.NormalizedNazwa2 == normalizedSearchTerm)
                return true;

            // Sprawdź kombinacje
            if (ulica.NormalizedCombined != null && ulica.NormalizedCombined == normalizedSearchTerm)
                return true;

            if (ulica.NormalizedCombinedReverse != null && ulica.NormalizedCombinedReverse == normalizedSearchTerm)
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
            // Chroni przed false positive: "Powstańców" nie znajdzie "Powstańców Śląskich"
            foreach (var ulica in ulice)
            {
                if (IsPartialMatch(ulica.NormalizedNazwa1, normalized))
                    return ulica;

                if (ulica.NormalizedNazwa2 != null && IsPartialMatch(ulica.NormalizedNazwa2, normalized))
                    return ulica;
            }

            return null;
        }

        /// <summary>
        /// 🆕 Znajduje ulicę TYLKO metodą dokładnego dopasowania (bez partial match)
        /// Używane gdy priorytetem jest precyzja (np. "Powstańców" nie może znaleźć "Powstańców Śląskich")
        /// </summary>
        public UlicaCached? FindStreetExact(List<UlicaCached> ulice, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return null;

            var normalizedSearch = _normalizer.Normalize(searchTerm);

            foreach (var ulica in ulice)
            {
                // 1. Sprawdź Nazwa1 (główna nazwa ulicy)
                if (ulica.NormalizedNazwa1 == normalizedSearch)
                    return ulica;

                // 2. Sprawdź Combined (Nazwa2 + " " + Nazwa1)
                if (!string.IsNullOrEmpty(ulica.NormalizedCombined) && 
                    ulica.NormalizedCombined == normalizedSearch)
                    return ulica;

                // 3. Sprawdź CombinedReverse (Nazwa1 + " " + Nazwa2)
                if (!string.IsNullOrEmpty(ulica.NormalizedCombinedReverse) && 
                    ulica.NormalizedCombinedReverse == normalizedSearch)
                    return ulica;

                // 🆕 4. Sprawdź inicjał z Nazwa2 (np. "j lea" pasuje do Nazwa2="juliusza" + Nazwa1="lea")
                if (!string.IsNullOrEmpty(ulica.Nazwa2))
                {
                    if (MatchesInitial(searchTerm, ulica.Nazwa2, ulica.Nazwa1))
                        return ulica;
                }
            }

            return null;
        }

        /// <summary>
        /// Sprawdza czy searchTerm zawiera inicjał (np. "J.Lea" pasuje do "Juliusza Lea")
        /// </summary>
        private bool MatchesInitial(string searchTerm, string nazwa2, string nazwa1)
        {
            // Wzorzec: "J.Lea", "j.lea", "J. Lea"
            var match = System.Text.RegularExpressions.Regex.Match(searchTerm, @"^([A-Za-z])\.?\s*(.+)$");
            
            if (!match.Success)
                return false;

            var initial = match.Groups[1].Value.ToLowerInvariant();
            var restOfName = match.Groups[2].Value;

            // Sprawdź czy inicjał pasuje do pierwszej litery Nazwa2
            var nazwa2Normalized = _normalizer.Normalize(nazwa2);
            if (!nazwa2Normalized.StartsWith(initial))
                return false;

            // Sprawdź czy reszta pasuje do Nazwa1
            var restNormalized = _normalizer.Normalize(restOfName);
            var nazwa1Normalized = _normalizer.Normalize(nazwa1);
            
            return restNormalized == nazwa1Normalized;
        }

        /// <summary>
        /// Dopasowanie częściowe - sprawdza czy searchTerm jest CAŁYM SŁOWEM w nazwie ulicy
        /// </summary>
        private bool IsPartialMatch(string normalizedStreetName, string searchTerm)
        {
            // Split tylko raz, bez dodatkowej normalizacji
            var words = normalizedStreetName.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            return Array.IndexOf(words, searchTerm) >= 0;
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

                int distanceReverse = int.MaxValue;
                if (!string.IsNullOrEmpty(ulica.NormalizedCombinedReverse))
                {
                    distanceReverse = LevenshteinDistance(normalizedSearch, ulica.NormalizedCombinedReverse);
                }

                int minDistance = Math.Min(distance1, Math.Min(distanceCombined, distanceReverse));

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
                double minSimilarity = normalizedSearch.Length <= 5 ? 0.7 : 0.5; // 70% dla ≤5 znaków, 50% dla dłuższych
                
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