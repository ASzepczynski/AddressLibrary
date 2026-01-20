// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Collections.Generic;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// 🎯 CENTRALNA DEFINICJA STATUSÓW WYSZUKIWANIA
    /// Słownik mapujący status → komunikat błędu
    /// </summary>
    public static class AddressSearchStatusInfo
    {
        /// <summary>
        /// Słownik komunikatów dla każdego statusu
        /// </summary>
        private static readonly Dictionary<AddressSearchStatus, string> StatusMessages = new()
        {
            { AddressSearchStatus.Success, "Znaleziono adres" },
            { AddressSearchStatus.MultipleMatches, "Znaleziono wiele dopasowań" },
            { AddressSearchStatus.MiastoNotFound, "Nie znaleziono miejscowości" },
            { AddressSearchStatus.UlicaNotFound, "Nie znaleziono ulicy" },
            { AddressSearchStatus.InvalidStreetName, "Błędna nazwa ulicy" },
            { AddressSearchStatus.KodPocztowyNotFound, "Nie znaleziono kodu pocztowego dla podanych parametrów" },
            { AddressSearchStatus.ValidationError, "Błąd walidacji danych wejściowych" }
        };

        /// <summary>
        /// ✅ Pobiera komunikat dla danego statusu
        /// </summary>
        public static string GetMessage(AddressSearchStatus status, string? customDetail = null)
        {
            if (!StatusMessages.TryGetValue(status, out var baseMessage))
            {
                return $"Nieznany status wyszukiwania: {status}";
            }

            // Jeśli podano szczegóły (np. nazwa ulicy), dołącz je
            return string.IsNullOrEmpty(customDetail)
                ? baseMessage
                : $"{baseMessage} '{customDetail}'";
        }

        /// <summary>
        /// ✅ Sprawdza czy status oznacza sukces
        /// </summary>
        public static bool IsSuccess(AddressSearchStatus status)
        {
            return status == AddressSearchStatus.Success;
        }

        /// <summary>
        /// ✅ Sprawdza czy status oznacza błąd (nie sukces, nie wiele dopasowań)
        /// </summary>
        public static bool IsError(AddressSearchStatus status)
        {
            return status != AddressSearchStatus.Success 
                && status != AddressSearchStatus.MultipleMatches;
        }

        /// <summary>
        /// ✅ Sprawdza czy status wymaga podania szczegółów (np. nazwy ulicy)
        /// </summary>
        public static bool RequiresDetail(AddressSearchStatus status)
        {
            return status == AddressSearchStatus.UlicaNotFound
                || status == AddressSearchStatus.InvalidStreetName
                || status == AddressSearchStatus.MiastoNotFound;
        }
    }
}