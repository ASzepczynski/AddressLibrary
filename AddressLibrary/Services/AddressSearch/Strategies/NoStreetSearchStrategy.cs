// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
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
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log("");
            diagnostic?.Log("--- STRATEGIA: Szukanie bez ulicy ---");

            var (selectedMiasto, wasFuzzyPostalCode) = SelectCityWithMethod(request, miasta, diagnostic);

            if (selectedMiasto == null)
            {
                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.MiastoNotFound,
                    Message = GetCityNotFoundMessage(miasta, request)
                };
                result.AddDiagnostic($"Szukane miasto: {request.Miasto}");
                result.AddDiagnostic($"Znaleziono {miasta.Count} miast o tej nazwie");
                if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
                    result.AddDiagnostic($"Kod pocztowy: {request.KodPocztowy}");
                result.AddDiagnostic("Nie można jednoznacznie określić miasta");
                return result;
            }

            diagnostic?.Log($"Wybrano miasto: {selectedMiasto.Nazwa} (ID: {selectedMiasto.Id})");

            // Znajdź kody pocztowe
            if (!_cache.TryGetKodyPocztoweMiasta(selectedMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miasta ID: {selectedMiasto.Id}");

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = selectedMiasto,
                    Message = $"Brak kodów pocztowych dla miasta {request.Miasto}",
                    NormalizedBuildingNumber = request.NumerDomu,
                    NormalizedApartmentNumber = request.NumerMieszkania
                };
                result.AddDiagnostic($"Miasto: {selectedMiasto.Nazwa}");
                result.AddDiagnostic("Miasto nie ma kodów pocztowych");

                // ✅ NOWE: Oznacz jako fuzzy jeśli kod pocztowy był podobny
                if (wasFuzzyPostalCode)
                {
                    result.PostalCodeMatchingMethod = MatchingMethod.Fuzzy;
                    result.AddMatchingDetail($"Kod pocztowy: podobny (pierwsze 3 cyfry z '{request.KodPocztowy}')");
                }

                return result;
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miasta");

            // Filtruj tylko kody bez ulicy
            var filteredKody = _filters.FilterWithoutStreet(kodyPocztowe);
            diagnostic?.Log($"Po filtracji bez ulicy: {filteredKody.Count} kodów");

            // Filtruj po numerze domu
            if (!string.IsNullOrWhiteSpace(request.NumerDomu))
            {
                var newFilteredKody = _filters.FilterByBuildingNumber(filteredKody, request.NumerDomu);
                diagnostic?.Log($"Po filtracji po numerze domu '{request.NumerDomu}': {newFilteredKody.Count} kodów (było: {filteredKody.Count()})");
                filteredKody = newFilteredKody;
            }

            var finalResult = _resultFactory.CreateResult(filteredKody, selectedMiasto, null, request.NumerDomu, request.NumerMieszkania, diagnostic);

            // ✅ NOWE: Oznacz jako fuzzy jeśli kod pocztowy był podobny
            if (wasFuzzyPostalCode)
            {
                finalResult.PostalCodeMatchingMethod = MatchingMethod.Fuzzy;
                finalResult.AddMatchingDetail($"Kod pocztowy: podobny (pierwsze 3 cyfry z '{request.KodPocztowy}')");
            }

            return finalResult;
        }

        private (Miasto? miasto, bool wasFuzzyPostalCode) SelectCityWithMethod(
            AddressSearchRequest request,
            List<Miasto> miasta,
            GeneralLogger? diagnostic)
        {
            // Jeśli mamy wiele miast
            if (miasta.Count > 1)
            {
                diagnostic?.Log($"Znaleziono {miasta.Count} miast o nazwie '{request.Miasto}'");

                // Próbuj zawęzić po kodzie pocztowym
                if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
                {
                    var (cityByCode, wasFuzzy) = SelectCityByPostalCode(request, miasta, diagnostic);
                    if (cityByCode != null)
                    {
                        return (cityByCode, wasFuzzy);
                    }
                    // Jeśli kod nie pomógł - błąd niejednoznaczności
                    diagnostic?.Log($"✗ Nie można jednoznacznie określić miasta - kod pocztowy nie pasuje do żadnego z {miasta.Count} miast");
                }
                else
                {
                    diagnostic?.Log($"✗ Nie można jednoznacznie określić miasta - brak kodu pocztowego");
                }

                // NIE zwracamy pierwszego miasta - zwracamy null
                return (null, false);
            }

            // KROK 1: Tylko jedno miasto - użyj go
            if (miasta.Count == 1)
            {
                diagnostic?.Log($"✓ Tylko jedno miasto po normalizacji: {miasta[0].Nazwa}");
                return (miasta[0], false);
            }

            return (null, false);
        }

        private (Miasto? miasto, bool wasFuzzy) SelectCityByPostalCode(
            AddressSearchRequest request,
            List<Miasto> miasta,
            GeneralLogger? diagnostic)
        {
            var kodNorm = UliceUtils.NormalizujKodPocztowy(request.KodPocztowy);
            diagnostic?.Log($"Znaleziono {miasta.Count} miast o nazwie '{request.Miasto}', próba zawężenia po kodzie: {kodNorm}");

            var miastaZKodem = new List<Miasto>();
            var miastaZPodobnymKodem = new List<Miasto>();

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetKodyPocztoweMiasta(miasto.Id, out var kody))
                {
                    bool foundExact = false;
                    bool foundSimilar = false;

                    for (int i = 0; i < kody.Count; i++)
                    {
                        // Dokładne dopasowanie (wszystkie 5 cyfr)
                        if (!foundExact && kody[i].Kod == kodNorm)
                        {
                            miastaZKodem.Add(miasto);
                            foundExact = true;
                            break; // Nie sprawdzaj dalej - znaleźliśmy dokładne dopasowanie
                        }

                        // ✅ Podobne dopasowanie (pierwsze 3 cyfry: "XX-X")
                        if (!foundSimilar && !foundExact && kodNorm.Length >= 4 && kody[i].Kod.Length >= 4)
                        {
                            // Porównaj pierwsze 4 znaki (np. "12-3" == "12-3")
                            if (kody[i].Kod.Substring(0, 4) == kodNorm.Substring(0, 4))
                            {
                                miastaZPodobnymKodem.Add(miasto);
                                foundSimilar = true;
                            }
                        }
                    }
                }
            }

            // KROK 1: Jeśli jest dokładne dopasowanie - użyj go
            if (miastaZKodem.Count == 1)
            {
                diagnostic?.Log($"✓ Wybrano miasto po dokładnym kodzie pocztowym: {miastaZKodem[0].Nazwa} (woj. {miastaZKodem[0].Gmina?.Powiat?.Wojewodztwo?.Nazwa})");
                return (miastaZKodem[0], false); // ✅ Nie jest fuzzy
            }
            else if (miastaZKodem.Count > 1)
            {
                diagnostic?.Log($"✗ Znaleziono {miastaZKodem.Count} miast z dokładnym kodem {kodNorm}");
                return (null, false);
            }

            // KROK 2: Spróbuj z podobnym kodem (pierwsze 3 cyfry)
            if (miastaZPodobnymKodem.Count == 1)
            {
                diagnostic?.Log($"✓ Wybrano miasto po podobnym kodzie pocztowym (pierwsze 3 cyfry): {miastaZPodobnymKodem[0].Nazwa} (woj. {miastaZPodobnymKodem[0].Gmina?.Powiat?.Wojewodztwo?.Nazwa})");
                return (miastaZPodobnymKodem[0], true); // ✅ Jest fuzzy!
            }
            else if (miastaZPodobnymKodem.Count > 1)
            {
                diagnostic?.Log($"✗ Znaleziono {miastaZPodobnymKodem.Count} miast z podobnym kodem (pierwsze 3 cyfry z {kodNorm})");
                return (null, false);
            }

            diagnostic?.Log($"✗ Żadne z {miasta.Count} miast nie ma kodu {kodNorm} (ani podobnego)");
            return (null, false);
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