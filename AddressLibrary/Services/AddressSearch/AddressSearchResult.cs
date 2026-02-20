// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using System.Collections.Generic;
using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Status wyszukiwania adresu
    /// </summary>
    public enum AddressSearchStatus
    {
        Success,              // Znaleziono dokładny adres
        MultipleMatches,      // Znaleziono wiele pasujących adresów
        MiastoNotFound,       // Nie znaleziono miasta/miejscowości
        UlicaNotFound,        // Nie znaleziono ulicy
        InvalidStreetName,    // Błędna nazwa ulicy
        KodPocztowyNotFound,  // Nie znaleziono kodu pocztowego
        ValidationError       // Błąd walidacji danych wejściowych
    }

    /// <summary>
    /// Wynik wyszukiwania adresu
    /// </summary>
    public class AddressSearchResult
    {
        public AddressSearchStatus Status { get; set; }
        public string? Message { get; set; }

        // Znalezione dane
        public KodPocztowy? KodPocztowy { get; set; }
        public Miasto? Miasto { get; set; }
        public Ulica? Ulica { get; set; }

        // Znormalizowane numery (z uwzględnieniem numerów wyciągniętych z nazwy ulicy)
        public string? NormalizedBuildingNumber { get; set; }
        public string? NormalizedApartmentNumber { get; set; }

        // W przypadku wielu dopasowań
        public List<KodPocztowy>? AlternativeMatches { get; set; }

        // Informacje diagnostyczne
        public string? DiagnosticInfo { get; set; }

        // ✅ NOWE: Metody dopasowania dla poszczególnych komponentów
        public MatchingMethod? CityMatchingMethod { get; set; }
        public MatchingMethod? StreetMatchingMethod { get; set; }
        public MatchingMethod? PostalCodeMatchingMethod { get; set; }

        // ✅ NOWE: Dodatkowe flagi
        public bool WasCityStreetSwapped { get; set; } = false;

        // ✅ NOWE: Szczegóły dopasowania
        private List<string> _matchingDetails = new();

        public void AddMatchingDetail(string detail)
        {
            _matchingDetails.Add(detail);
        }

        public string GetMatchingDetails()
        {
            if (_matchingDetails.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var detail in _matchingDetails)
            {
                sb.AppendLine($"  • {detail}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// ✅ Zwraca ogólną metodę dopasowania (Fuzzy jeśli którykolwiek komponent był fuzzy)
        /// </summary>
        public MatchingMethod GetOverallMethod()
        {
            if (CityMatchingMethod == MatchingMethod.Fuzzy ||
                StreetMatchingMethod == MatchingMethod.Fuzzy ||
                PostalCodeMatchingMethod == MatchingMethod.Fuzzy ||
                WasCityStreetSwapped)
            {
                return MatchingMethod.Fuzzy;
            }

            return MatchingMethod.Strict;
        }

        /// <summary>
        /// ✅ Helper do dodawania diagnostyki
        /// </summary>
        public void AddDiagnostic(string message)
        {
            if (string.IsNullOrEmpty(DiagnosticInfo))
            {
                DiagnosticInfo = message;
            }
            else
            {
                DiagnosticInfo += "\n" + message;
            }
        }
    }
}