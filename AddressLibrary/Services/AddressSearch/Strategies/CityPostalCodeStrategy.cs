// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch.Filters;

namespace AddressLibrary.Services.AddressSearch.Strategies
{
    /// <summary>
    /// Strategia zwracania kodu pocztowego miasta gdy ulica nie ma przypisanego kodu
    /// </summary>
    public class CityPostalCodeStrategy
    {
        private readonly AddressSearchCache _cache;
        private readonly PostalCodeFilters _filters;

        public CityPostalCodeStrategy(AddressSearchCache cache, PostalCodeFilters filters)
        {
            _cache = cache;
            _filters = filters;
        }

        public AddressSearchResult Execute(
            AddressSearchRequest request,
            Miasto miasto,
            Ulica ulica,
            string normalizedBuildingNumber,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Zwracanie kodu miasta dla ulicy bez kodu ---");

            if (!_cache.TryGetKodyPocztowe(miasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log("✗ Brak kodów pocztowych dla miejscowości");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            var cityCode = _filters.FindCityPostalCode(kodyPocztowe);

            if (cityCode != null)
            {
                diagnostic?.Log($"✓ Zwracam kod miejscowości: {cityCode.Kod} (ulica nie ma przypisanego kodu)");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = cityCode,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = null,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }
            else
            {
                diagnostic?.Log("✗ Nie znaleziono kodu miejscowości (wszystkie kody mają przypisaną ulicę)");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }
        }
    }
}