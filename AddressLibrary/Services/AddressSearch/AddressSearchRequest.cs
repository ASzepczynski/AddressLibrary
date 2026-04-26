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
        public string KodPocztowy { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa miejscowoœci (wymagana)
        /// </summary>
        public string Miasto { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa ulicy (opcjonalna)
        /// </summary>
        public string Ulica { get; set; } = string.Empty;

        /// <summary>
        /// Numer domu (opcjonalny)
        /// </summary>
        public string NumerDomu { get; set; } = string.Empty;

        /// <summary>
        /// Numer mieszkania (opcjonalny)
        /// </summary>
        public string NumerMieszkania { get; set; } = string.Empty;
    }
}