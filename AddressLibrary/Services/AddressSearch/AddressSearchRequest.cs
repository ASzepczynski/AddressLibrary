// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Rekord wyszukiwania adresu
    /// </summary>
    public record AddressSearchRequest
    {
        /// <summary>
        /// Kod pocztowy (opcjonalny)
        /// </summary>
        public string? KodPocztowy { get; init; }

        /// <summary>
        /// Nazwa miejscowoœci (wymagana)
        /// </summary>
        public string Miejscowosc { get; init; } = string.Empty;

        /// <summary>
        /// Nazwa ulicy (opcjonalna)
        /// </summary>
        public string? Ulica { get; init; }

        /// <summary>
        /// Numer domu (opcjonalny)
        /// </summary>
        public string? NumerDomu { get; init; }

        /// <summary>
        /// Numer mieszkania (opcjonalny)
        /// </summary>
        public string? NumerMieszkania { get; init; }
    }
}