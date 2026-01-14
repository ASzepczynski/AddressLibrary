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
        public UlicaCached? FindStreetExact(List<UlicaCached> ulice, string originalStreetName)
        {
            // ✅ KROK 1: Dokładne dopasowanie z oryginalną nazwą
            var normalized = _normalizer.Normalize(originalStreetName);
            
            foreach (var ulica in ulice)
            {
                if (IsMatch(ulica, normalized))
                    return ulica;
            }

            // ✅ KROK 2: Retry bez skrótu imienia
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

            // ❌ BEZ dopasowania częściowego - tylko dokładne
            return null;
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
    }
}