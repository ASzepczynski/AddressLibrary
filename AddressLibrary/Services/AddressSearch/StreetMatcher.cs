// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do dopasowywania nazw ulic
    /// </summary>
    public class StreetMatcher
    {
        private readonly TextNormalizer _normalizer;

        public StreetMatcher(TextNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        /// <summary>
        /// Sprawdza czy ulica pasuje do wyszukiwanego terminu
        /// </summary>
        public bool IsMatch(string nazwa1, string? nazwa2, string searchTerm)
        {
            if (IsNameMatch(nazwa1, searchTerm))
                return true;

            if (!string.IsNullOrEmpty(nazwa2) && IsNameMatch(nazwa2, searchTerm))
                return true;

            if (!string.IsNullOrEmpty(nazwa2))
            {
                var combined = _normalizer.Normalize($"{nazwa2} {nazwa1}");
                if (combined == searchTerm)
                    return true;
            }

            if (!string.IsNullOrEmpty(nazwa2))
            {
                var combinedReverse = _normalizer.Normalize($"{nazwa1} {nazwa2}");
                if (combinedReverse == searchTerm)
                    return true;
            }

            return false;
        }

        private bool IsNameMatch(string streetNameInDb, string searchTerm)
        {
            var normalized = _normalizer.Normalize(streetNameInDb);

            if (normalized == searchTerm)
                return true;

            var words = normalized.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Contains(searchTerm);
        }
    }
}