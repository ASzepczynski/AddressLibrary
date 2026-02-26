// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Logging;
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
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log("");
            diagnostic?.Log("--- STRATEGIA: Zwracanie kodu miasta dla ulicy bez kodu ---");

            if (!_cache.TryGetKodyPocztoweMiasta(miasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log("✗ Brak kodów pocztowych dla miejscowości");

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania
                };
                result.AddDiagnostic($"Miasto: {miasto.Nazwa}");
                result.AddDiagnostic($"Ulica: {ulica.Nazwa1}");
                result.AddDiagnostic("Miejscowość nie ma kodów pocztowych");
                return result;
            }

            var cityCode = _filters.FindCityPostalCode(kodyPocztowe);

            if (cityCode != null)
            {
                diagnostic?.Log($"✓ Zwracam kod miejscowości: {cityCode.Kod} (ulica nie ma przypisanego kodu)");

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = cityCode,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = null,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania
                };
                result.AddDiagnostic($"Kod: {cityCode.Kod}");
                result.AddDiagnostic($"Miasto: {miasto.Nazwa}");
                result.AddDiagnostic($"Ulica: {ulica.Nazwa1}");
                result.AddDiagnostic("Ulica nie ma przypisanego kodu - zwrócono kod miasta");
                return result;
            }
            else
            {
                diagnostic?.Log($"✗ Brak kodu w PNA dla ulicy");

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = "Brak kodu pocztowego",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania
                };
                result.AddDiagnostic($"Miasto: {miasto.Nazwa}");
                result.AddDiagnostic($"Ulica: {ulica.Nazwa1}");
                result.AddDiagnostic("Brak kodu ulicy w PNA");
                return result;
            }
        }
    }
}