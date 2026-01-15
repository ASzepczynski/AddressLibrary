// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Fabryka do tworzenia wyników wyszukiwania
    /// </summary>
    public class SearchResultFactory
    {
        private readonly AddressSearchCache _cache;

        public SearchResultFactory(AddressSearchCache cache)
        {
            _cache = cache;
        }

        public AddressSearchResult CreateResult(
            List<KodPocztowy> kodyPocztowe,
            Miasto miasto,
            Ulica? ulica,
            string? normalizedBuildingNumber,
            string? normalizedApartmentNumber,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- TWORZENIE WYNIKU ---");

            if (kodyPocztowe.Count == 0)
            {
                diagnostic?.Log("✗ Nie znaleziono żadnych pasujących kodów pocztowych");
                
                // ✅ POPRAWKA: Bardziej opisowy komunikat gdy nie podano ulicy w mieście z ulicami
                string errorMessage;
                if (ulica == null && CityHasStreets(miasto.Id))
                {
                    errorMessage = $"W mieście '{miasto.Nazwa}' nie podano ulicy";
                }
                else
                {
                    errorMessage = "Nie znaleziono kodu pocztowego dla podanych parametrów";
                }

                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = errorMessage,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            if (kodyPocztowe.Count == 1)
            {
                var kod = kodyPocztowe[0];
                diagnostic?.Log($"Jedno dopasowanie: {kod.Kod}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = kod,
                    Miasto = miasto,
                    Ulica = ulica,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            diagnostic?.Log($"⚠ Znaleziono wiele dopasowań: {kodyPocztowe.Count}");
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Miasto = miasto,
                Ulica = ulica,
                KodPocztowy = kodyPocztowe[0],
                AlternativeMatches = kodyPocztowe,
                Message = $"Znaleziono wiele dopasowań ({kodyPocztowe.Count})",
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = normalizedApartmentNumber,
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

        /// <summary>
        /// Sprawdza czy miasto ma zdefiniowane ulice
        /// </summary>
        private bool CityHasStreets(int miastoId)
        {
            if (_cache.TryGetUlice(miastoId, out var ulice))
            {
                return ulice.Count > 0;
            }
            return false;
        }
    }
}