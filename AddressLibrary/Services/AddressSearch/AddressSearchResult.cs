// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Status wyszukiwania adresu
    /// </summary>
    public enum AddressSearchStatus
    {
        Success,              // Znaleziono dok³adny adres
        MultipleMatches,      // Znaleziono wiele pasuj¹cych adresów
        MiastoNotFound,  // Nie znaleziono miejscowoœci
        UlicaNotFound,        // Nie znaleziono ulicy
        KodPocztowyNotFound,  // Nie znaleziono kodu pocztowego
        ValidationError       // B³¹d walidacji danych wejœciowych
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

        // Znormalizowane numery (z uwzglêdnieniem numerów wyci¹gniêtych z nazwy ulicy)
        public string? NormalizedBuildingNumber { get; set; }
        public string? NormalizedApartmentNumber { get; set; }

        // W przypadku wielu dopasowañ
        public List<KodPocztowy>? AlternativeMatches { get; set; }

        // Informacje diagnostyczne
        public string? DiagnosticInfo { get; set; }
    }
}