// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Helpers;
using AddressLibrary.Services.AddressSearch.Filters;

namespace AddressLibrary.Services.AddressSearch.Strategies
{
    /// <summary>
    /// Strategia wyszukiwania adresu bez podanej ulicy
    /// </summary>
    public class NoStreetSearchStrategy
    {
        private readonly AddressSearchCache _cache;
        private readonly TextNormalizer _normalizer;
        private readonly PostalCodeFilters _filters;
        private readonly SearchResultFactory _resultFactory;

        public NoStreetSearchStrategy(
            AddressSearchCache cache,
            TextNormalizer normalizer,
            PostalCodeFilters filters,
            SearchResultFactory resultFactory)
        {
            _cache = cache;
            _normalizer = normalizer;
            _filters = filters;
            _resultFactory = resultFactory;
        }

        public AddressSearchResult Execute(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Szukanie bez ulicy ---");

            var selectedMiasto = SelectCity(request, miasta, diagnostic);

            if (selectedMiasto == null)
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.MiastoNotFound,
                    Message = GetCityNotFoundMessage(miasta, request),
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            diagnostic?.Log($"Wybrano miasto: {selectedMiasto.Nazwa} (ID: {selectedMiasto.Id})");

            // Znajdź kody pocztowe
            if (!_cache.TryGetKodyPocztowe(selectedMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miasta ID: {selectedMiasto.Id}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = selectedMiasto,
                    Message = $"Brak kodów pocztowych dla miasta {request.Miasto}",
                    NormalizedBuildingNumber = request.NumerDomu,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miasta");

            // Filtruj tylko kody bez ulicy
            var filteredKody = _filters.FilterWithoutStreet(kodyPocztowe);
            diagnostic?.Log($"Po filtracji bez ulicy: {filteredKody.Count} kodów");

            // Filtruj po numerze domu
            if (!string.IsNullOrWhiteSpace(request.NumerDomu))
            {
                var beforeFilter = filteredKody.Count;
                filteredKody = _filters.FilterByBuildingNumber(filteredKody, request.NumerDomu);
                diagnostic?.Log($"Po filtracji po numerze domu '{request.NumerDomu}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            return _resultFactory.CreateResult(filteredKody, selectedMiasto, null, request.NumerDomu, request.NumerMieszkania, diagnostic);
        }

        private Miasto? SelectCity(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            // Jeśli mamy wiele miast
            if (miasta.Count > 1)
            {
                diagnostic?.Log($"Znaleziono {miasta.Count} miast o nazwie '{request.Miasto}'");
                
                // Próbuj zawęzić po kodzie pocztowym
                if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
                {
                    var cityByCode = SelectCityByPostalCode(request, miasta, diagnostic);
                    if (cityByCode != null)
                    {
                        return cityByCode;
                    }
                    // Jeśli kod nie pomógł - błąd niejednoznaczności
                    diagnostic?.Log($"✗ Nie można jednoznacznie określić miasta - kod pocztowy nie pasuje do żadnego z {miasta.Count} miast");
                }
                else
                {
                    diagnostic?.Log($"✗ Nie można jednoznacznie określić miasta - brak kodu pocztowego");
                }
                
                // NIE zwracamy pierwszego miasta - zwracamy null
                return null;
            }

            // KROK 1: Tylko jedno miasto - użyj go
            if (miasta.Count == 1)
            {
                diagnostic?.Log($"✓ Tylko jedno miasto po normalizacji: {miasta[0].Nazwa}");
                return miasta[0];
            }

            return null;
        }

        private Miasto? SelectCityByPostalCode(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            var kodNorm = UliceUtils.NormalizujKodPocztowy(request.KodPocztowy);
            diagnostic?.Log($"Znaleziono {miasta.Count} miast o nazwie '{request.Miasto}', próba zawężenia po kodzie: {kodNorm}");

            var miastaZKodem = new List<Miasto>();

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetKodyPocztowe(miasto.Id, out var kody))
                {
                    for (int i = 0; i < kody.Count; i++)
                    {
                        if (kody[i].Kod == kodNorm)
                        {
                            miastaZKodem.Add(miasto);
                            break;
                        }
                    }
                }
            }

            if (miastaZKodem.Count == 1)
            {
                diagnostic?.Log($"✓ Wybrano miasto po kodzie pocztowym: {miastaZKodem[0].Nazwa} (woj. {miastaZKodem[0].Gmina?.Powiat?.Wojewodztwo?.Nazwa})");
                return miastaZKodem[0];
            }
            else if (miastaZKodem.Count > 1)
            {
                diagnostic?.Log($"✗ Znaleziono {miastaZKodem.Count} miast z kodem {kodNorm}");
                return null;
            }
            else
            {
                diagnostic?.Log($"✗ Żadne z {miasta.Count} miast nie ma kodu {kodNorm}");
                return null;
            }
        }

        private string GetCityNotFoundMessage(List<Miasto> miasta, AddressSearchRequest request)
        {
            if (miasta.Count > 1)
            {
                if (string.IsNullOrWhiteSpace(request.KodPocztowy))
                {
                    return $"Znaleziono {miasta.Count} miast o nazwie '{request.Miasto}'. Podaj ulicę, kod pocztowy, województwo lub powiat aby zawęzić wyniki.";
                }
                else
                {
                    return $"Kod pocztowy {request.KodPocztowy} nie pasuje do żadnego miasta o nazwie '{request.Miasto}'";
                }
            }
            else
            {
                return $"Nie znaleziono miasta '{request.Miasto}' (bez ulicy wymagane jest dokładne dopasowanie)";
            }
        }
    }
}