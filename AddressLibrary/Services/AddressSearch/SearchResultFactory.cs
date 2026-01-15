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
                diagnostic?.Log($"✓ Jedno dopasowanie: {kod.Kod}");
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

            // 🆕 WIELE DOPASOWAŃ: Pokaż kody pocztowe + nazwy ulic
            diagnostic?.Log($"⚠ Znaleziono wiele dopasowań: {kodyPocztowe.Count}");
            
            // Pobierz ulice z cache
            if (!_cache.TryGetUlice(miasto.Id, out var cachedUlice))
            {
                diagnostic?.Log($"⚠ Nie udało się pobrać ulic z cache dla miasta {miasto.Nazwa} (ID={miasto.Id})");
            }
            else
            {
                diagnostic?.Log($"✓ Pobrano {cachedUlice.Count} ulic z cache dla miasta {miasto.Nazwa}");
            }

            // Zbierz informacje o kodach pocztowych
            var postalCodeInfoList = new List<string>();
            var processedCodes = new HashSet<string>(); // Zapobiegamy duplikatom kodów

            foreach (var kod in kodyPocztowe)
            {
                diagnostic?.Log($"  Kod: {kod.Kod}, UlicaId: {kod.UlicaId?.ToString() ?? "NULL"}");

                if (processedCodes.Add(kod.Kod)) // Dodaj tylko unikalne kody
                {
                    string codeInfo = kod.Kod;

                    // Dodaj nazwę ulicy jeśli dostępna
                    if (kod.UlicaId.HasValue && cachedUlice != null)
                    {
                        var street = cachedUlice.FirstOrDefault(u => u.Id == kod.UlicaId.Value);
                        if (street != null)
                        {
                            var streetName = !string.IsNullOrEmpty(street.Cecha)
                                ? $"{street.Cecha} {street.Nazwa1}".Trim()
                                : street.Nazwa1;
                            codeInfo = $"{kod.Kod} ({streetName})";
                            diagnostic?.Log($"    ✓ {codeInfo}");
                        }
                    }

                    postalCodeInfoList.Add(codeInfo);
                }
            }

            // Utwórz komunikat
            string message;
            if (postalCodeInfoList.Count > 0)
            {
                var codeList = string.Join(", ", postalCodeInfoList);
                message = $"Znaleziono wiele dopasowań ({postalCodeInfoList.Count}): {codeList}";
                diagnostic?.Log($"  ✓ Komunikat: {message}");
            }
            else
            {
                message = $"Znaleziono wiele dopasowań ({kodyPocztowe.Count})";
                diagnostic?.Log($"  ⚠ Nie udało się utworzyć szczegółowego komunikatu");
            }

            return new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Miasto = miasto,
                Ulica = ulica,
                KodPocztowy = kodyPocztowe[0],
                AlternativeMatches = kodyPocztowe,
                Message = message,
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = normalizedApartmentNumber,
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

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