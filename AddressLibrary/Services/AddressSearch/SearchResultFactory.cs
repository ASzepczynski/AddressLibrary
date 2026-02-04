// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Logging;

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
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log("");
            diagnostic?.Log("--- TWORZENIE WYNIKU ---");

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

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = errorMessage,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber
                };

                // ✅ Dodaj diagnostykę zamiast całego logu
                result.AddDiagnostic($"Miasto: {miasto.Nazwa} (ID={miasto.Id})");
                if (ulica != null)
                    result.AddDiagnostic($"Ulica: {ulica.Nazwa1} (ID={ulica.Id})");
                result.AddDiagnostic($"Numer budynku: {normalizedBuildingNumber ?? "brak"}");
                result.AddDiagnostic($"Liczba znalezionych kodów: 0");
                
                return result;
            }

            if (kodyPocztowe.Count == 1)
            {
                var kod = kodyPocztowe[0];
                diagnostic?.Log($"✓ Jedno dopasowanie: {kod.Kod}");
                
                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = kod,
                    Miasto = miasto,
                    Ulica = ulica,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber
                };

                // ✅ Dodaj diagnostykę
                result.AddDiagnostic($"Znaleziono: {kod.Kod}");
                result.AddDiagnostic($"Miasto: {miasto.Nazwa}");
                if (ulica != null)
                    result.AddDiagnostic($"Ulica: {ulica.Nazwa1}");
                
                return result;
            }

            // 🆕 WIELE DOPASOWAŃ: Pokaż kody pocztowe + ORYGINALNE nazwy ulic
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

            // Zbierz informacje o kodach pocztowych z ORYGINALNYMI nazwami ulic
            var postalCodeInfoList = new List<string>();
            var processedCodes = new HashSet<string>(); // Zapobiegamy duplikatom kodów

            foreach (var kod in kodyPocztowe)
            {
                diagnostic?.Log($"  Kod: {kod.Kod}, UlicaId: {kod.UlicaId}");

                if (processedCodes.Add(kod.Kod)) // Dodaj tylko unikalne kody
                {
                    string codeInfo = kod.Kod;

                    // Dodaj nazwę ulicy jeśli dostępna
                    if (cachedUlice != null)
                    {
                        var street = cachedUlice.FirstOrDefault(u => u.Id == kod.UlicaId);
                        if (street != null)
                        {
                            // 🆕 Użyj oryginalnej nazwy z cache (nieznormalizowanej)
                            var streetName = _cache.GetOriginalStreetName(street);
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

            var multiResult = new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Miasto = miasto,
                Ulica = ulica,
                KodPocztowy = kodyPocztowe[0],
                AlternativeMatches = kodyPocztowe,
                Message = message,
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = normalizedApartmentNumber
            };

            // ✅ Dodaj diagnostykę
            multiResult.AddDiagnostic($"Liczba dopasowań: {kodyPocztowe.Count}");
            multiResult.AddDiagnostic($"Miasto: {miasto.Nazwa}");
            if (ulica != null)
                multiResult.AddDiagnostic($"Ulica: {ulica.Nazwa1}");
            
            foreach (var codeInfo in postalCodeInfoList.Take(5)) // Max 5 przykładów
            {
                multiResult.AddDiagnostic($"  • {codeInfo}");
            }
            
            if (postalCodeInfoList.Count > 5)
                multiResult.AddDiagnostic($"  ... i {postalCodeInfoList.Count - 5} więcej");

            return multiResult;
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
