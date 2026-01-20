// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do rozwiązywania niejednoznaczności przy wyszukiwaniu ulic
    /// </summary>
    public class AmbiguousStreetResolver
    {
        private readonly TextNormalizer _normalizer;

        public AmbiguousStreetResolver(TextNormalizer normalizer)
        {
            _normalizer = normalizer;
        }

        /// <summary>
        /// Próbuje rozwiązać niejednoznaczność i wybrać jedną ulicę
        /// </summary>
        /// <param name="matchingStreets">Lista pasujących ulic (UlicaCached)</param>
        /// <param name="originalStreetName">Oryginalna nazwa ulicy z danych źródłowych</param>
        /// <param name="postalCode">Kod pocztowy z danych źródłowych (może być pusty)</param>
        /// <param name="postalCodes">Lista kodów pocztowych dla miejscowości</param>
        /// <returns>Wybrana ulica lub null jeśli nie można rozwiązać niejednoznaczności</returns>
        public UlicaCached? ResolveAmbiguity(
            List<UlicaCached> matchingStreets,
            string originalStreetName,
            string? postalCode,
            List<KodPocztowy> postalCodes)
        {
            if (matchingStreets == null || matchingStreets.Count <= 1)
                return matchingStreets?.FirstOrDefault();

            // ✅ KROK 1: Odfiltruj ulice zaczynające się od "Park", "inne", "rondo"
            var filteredStreets = FilterOutSpecialPrefixes(matchingStreets);

            if (filteredStreets.Count == 1)
                return filteredStreets[0];

            if (filteredStreets.Count == 0)
                filteredStreets = matchingStreets; // Jeśli wszystkie zostały odfiltrowane, użyj oryginalnej listy

            // ✅ KROK 2: Sprawdź dokładne dopasowanie nazwy (1:1)
            var exactMatch = FindExactNameMatch(filteredStreets, originalStreetName);
            if (exactMatch != null)
                return exactMatch;

            // ✅ KROK 3: Jeśli podano kod pocztowy, sprawdź dopasowanie po kodzie
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                var normalizedPostalCode = _normalizer.NormalizePostalCode(postalCode);
                var codeMatch = FindByPostalCode(filteredStreets, normalizedPostalCode, postalCodes);
                if (codeMatch != null)
                    return codeMatch;
            }

            // ❌ Nie udało się rozwiązać niejednoznaczności
            return null;
        }

        /// <summary>
        /// Odfiltruj ulice zaczynające się od "Park", "inne", "rondo"
        /// </summary>
        private List<UlicaCached> FilterOutSpecialPrefixes(List<UlicaCached> streets)
        {
            var specialPrefixes = new[] { "park", "inne", "rondo" };

            return streets.Where(s =>
            {
                var cechaNormalized = s.Cecha?.ToLowerInvariant() ?? "";
                return !specialPrefixes.Any(prefix => cechaNormalized.StartsWith(prefix));
            }).ToList();
        }

        /// <summary>
        /// Znajdź dokładne dopasowanie nazwy (1:1)
        /// Sprawdza zarówno sam Nazwa1 jak i kombinację Nazwa2 + Nazwa1
        /// </summary>
        private UlicaCached? FindExactNameMatch(List<UlicaCached> streets, string originalStreetName)
        {
            var normalizedSearch = _normalizer.Normalize(originalStreetName);

            var exactMatches = new List<UlicaCached>();

            foreach (var street in streets)
            {
                // ✅ Sprawdź dokładne dopasowanie do NormalizedNazwa1
                if (street.NormalizedNazwa1 == normalizedSearch)
                {
                    exactMatches.Add(street);
                    continue;
                }

                // ✅ Sprawdź dokładne dopasowanie do NormalizedCombined
                if (street.NormalizedCombined != null && 
                    street.NormalizedCombined == normalizedSearch)
                {
                    exactMatches.Add(street);
                    continue;
                }

                // ✅ Sprawdź dopasowanie do pełnej nazwy (Cecha + Nazwa2 + Nazwa1)
                var fullName = GetFullStreetName(street);
                if (_normalizer.Normalize(fullName) == normalizedSearch)
                {
                    exactMatches.Add(street);
                }
            }

            // Zwróć tylko jeśli jest dokładnie jedno dopasowanie
            return exactMatches.Count == 1 ? exactMatches[0] : null;
        }

        /// <summary>
        /// Znajdź ulicę po dokładnym dopasowaniu kodu pocztowego
        /// </summary>
        private UlicaCached? FindByPostalCode(
            List<UlicaCached> streets,
            string normalizedPostalCode,
            List<KodPocztowy> postalCodes)
        {
            var matchingByCode = new List<UlicaCached>();

            foreach (var street in streets)
            {
                // Sprawdź czy ta ulica ma przypisany dokładnie ten kod pocztowy
                var streetCodes = postalCodes
                    .Where(k => k.UlicaId == street.Id)
                    .Select(k => k.Kod)
                    .Distinct()
                    .ToList();

                if (streetCodes.Contains(normalizedPostalCode))
                {
                    matchingByCode.Add(street);
                }
            }

            // Zwróć tylko jeśli jest dokładnie jedno dopasowanie
            return matchingByCode.Count == 1 ? matchingByCode[0] : null;
        }

        /// <summary>
        /// Zwraca pełną nazwę ulicy (Cecha + Nazwa1 [+ Nazwa2 jeśli liczebnik])
        /// ✅ POPRAWIONE: Nazwa2 tylko jeśli jest liczbą (3-go, II-go)
        /// </summary>
        private string GetFullStreetName(UlicaCached street)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(street.Cecha))
                parts.Add(street.Cecha);

            // ✅ ZMIANA: Najpierw Nazwa1
            parts.Add(street.Nazwa1);

            // ✅ ZMIANA: Nazwa2 TYLKO jeśli wygląda na liczebnik (3-go, II-go)
            if (!string.IsNullOrWhiteSpace(street.Nazwa2) && IsOrdinalNumber(street.Nazwa2))
            {
                // Normalizuj liczebnik (usuń "-go", "-tego")
                var normalizedNazwa2 = NormalizeOrdinalNumber(street.Nazwa2);
                parts.Insert(parts.Count - 1, normalizedNazwa2); // Wstaw PRZED Nazwa1
            }

            return string.Join(" ", parts);
        }
// Prawdziwa nazwa żeby wyświetlić duplikaty
        private string GetOriginalStreetName(UlicaCached street)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(street.Cecha))
                parts.Add(street.Cecha);

            parts.Add(street.Nazwa1);
            if (!string.IsNullOrWhiteSpace(street.Nazwa2))
            {
                parts.Add(street.Nazwa2);
            }
            return string.Join(" ", parts);
        }


        /// <summary>
        /// Sprawdza czy tekst wygląda na liczebnik porządkowy
        /// </summary>
        private bool IsOrdinalNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Sprawdź czy zawiera liczby arabskie (1-go, 29-go) lub rzymskie (II-go, III-go)
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+(-?(go|tego|cie))?$") ||
                   System.Text.RegularExpressions.Regex.IsMatch(text, @"^[IVXLCDM]+(-?(go|tego|cie))?$", 
                       System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Normalizuje liczebniki porządkowe (usuwa "-go", "-tego", "-cie")
        /// </summary>
        private string NormalizeOrdinalNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"-?(go|tego|cie)$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Trim();
        }

        /// <summary>
        /// Zwraca szczegółowy komunikat o niejednoznaczności
        /// </summary>
        public string GetAmbiguityMessage(
            List<UlicaCached> streets,
            List<KodPocztowy> postalCodes)
        {
            var codesPerStreet = streets.Select(s =>
            {
                var codes = postalCodes
                    .Where(k => k.UlicaId == s.Id)
                    .Select(k => k.Kod)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                var streetName = GetOriginalStreetName(s);
                
                if (codes.Count > 0)
                    return $"{string.Join(", ", codes)} ({streetName})";
                else
                    return $"(brak kodu) ({streetName})";
            }).ToList();

            return $"Znaleziono wiele dopasowań ({codesPerStreet.Count}): {string.Join(", ", codesPerStreet)}";
        }
    }
}