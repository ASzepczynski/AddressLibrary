// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Status wyszukiwania adresu
    /// </summary>
    public enum AddressSearchStatus
    {
        Success,              // Znaleziono dokładny adres
        MultipleMatches,      // Znaleziono wiele pasujących adresów
        MiastoNotFound,       // Nie znaleziono miejscowości
        UlicaNotFound,        // Nie znaleziono ulicy
        InvalidStreetName,    // Błędna nazwa ulicy (nie istnieje w całej bazie TERYT)
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
    }
}