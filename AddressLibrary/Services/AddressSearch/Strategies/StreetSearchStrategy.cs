// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Utils;

namespace AddressLibrary.Services.AddressSearch.Strategies
{
    /// <summary>
    /// Strategia wyszukiwania adresu z podaną ulicą
    /// </summary>
    public class StreetSearchStrategy
    {
        private readonly AddressSearchCache _cache;
        private readonly TextNormalizer _normalizer;
        private readonly StreetMatcher _streetMatcher;
        private readonly PostalCodeFilters _filters;
        private readonly CityPostalCodeStrategy _cityStrategy;
        private readonly SearchResultFactory _resultFactory;
        private readonly AmbiguousStreetResolver _ambiguityResolver;

        public StreetSearchStrategy(
            AddressSearchCache cache,
            TextNormalizer normalizer,
            StreetMatcher streetMatcher,
            PostalCodeFilters filters,
            CityPostalCodeStrategy cityStrategy,
            SearchResultFactory resultFactory,
            AmbiguousStreetResolver ambiguityResolver)
        {
            _cache = cache;
            _normalizer = normalizer;
            _streetMatcher = streetMatcher;
            _filters = filters;
            _cityStrategy = cityStrategy;
            _resultFactory = resultFactory;
            _ambiguityResolver = ambiguityResolver;
        }

        public AddressSearchResult Execute(
            AddressSearchRequest request,
            List<Miasto> miasta,
            GeneralLogger? searchLogger)
        {
            searchLogger?.Log("");
            searchLogger?.Log("--- STRATEGIA: Szukanie z ulicą ---");

            // Wyodrębnij prefiks z ulicy
            (var Prefix, var normalizedStreet) = UliceUtils.SplitStreetPrefix(request.Ulica);
            normalizedStreet = _normalizer.Normalize(normalizedStreet);
            searchLogger?.Log($"Normalizacja ulicy: '{request.Ulica}' -> '{Prefix}/{normalizedStreet}'");

            var combinedBuildingNumber = request.NumerDomu;

            // 🆕 KROK 1: Znajdź WSZYSTKIE pasujące ulice
            var (matchingStreets, wasStreetFuzzy) = FindAllMatchingStreetsWithMethod(
                request, miasta, Prefix, normalizedStreet, searchLogger);

            if (matchingStreets.Count == 0)
            {
                return HandleStreetNotFound(request, miasta, Prefix, normalizedStreet, searchLogger);
            }

            // 🆕 KROK 2: Rozwiązuj niejednoznaczność jeśli potrzeba
            if (matchingStreets.Count > 1)
            {
                searchLogger?.Log($"⚠ Znaleziono {matchingStreets.Count} pasujących ulic - próba rozwiązania niejednoznaczności");

                var resolvedStreet = ResolveAmbiguousStreets(request, matchingStreets, searchLogger);

                if (resolvedStreet == null)
                {
                    return CreateMultipleMatchesError(matchingStreets, miasta, searchLogger);
                }

                matchingStreets = new List<(UlicaCached street, Miasto miasto)> { resolvedStreet.Value };
                searchLogger?.Log($"✓ Rozwiązano niejednoznaczność: {_cache.GetOriginalStreetName(resolvedStreet.Value.street)}");
            }

            // 🆕 KROK 3: Kontynuuj z wybraną ulicą
            var (foundUlica, foundMiasto) = matchingStreets[0];
            searchLogger?.Log($"✓ Znaleziono dokładnie jedną ulicę: {_cache.GetOriginalStreetName(foundUlica)}");

            // Przekształć UlicaCached na Ulica
            var ulica = new Ulica
            {
                Id = foundUlica.Id,
                MiastoId = foundUlica.MiastoId,
                Cecha = foundUlica.Cecha,
                Nazwa1 = foundUlica.Nazwa1,
                Nazwa2 = foundUlica.Nazwa2,
                Miasto = foundUlica.Miasto
            };

            // Znajdź kody pocztowe
            if (!_cache.TryGetKodyPocztoweUlicy(ulica.Id, out var kodyPocztowe))
            {
                if (!_cache.TryGetKodyPocztoweMiasta(ulica.Miasto.Id, out var kodyPocztoweMiasta))
                {
                    searchLogger?.Log($"✗ Brak kodów pocztowych dla ulicy ID: {ulica.Id}");
                    var result = new AddressSearchResult
                    {
                        Status = AddressSearchStatus.KodPocztowyNotFound,
                        Message = $"Nie znaleziono kodów pocztowych dla {ulica.Id}",
                        Miasto = ulica.Miasto
                    };
                    return result;
                }
                // Czy miasto ma jeden kod?
                if (kodyPocztoweMiasta.Count != 1)
                {
                    searchLogger?.Log($"✗ To nie jest miasto z jednym kodem ulicaID: {ulica.Id}");
                    var result = new AddressSearchResult
                    {
                        Status = AddressSearchStatus.KodPocztowyNotFound,
                        Message = $"Nie znaleziono kodów pocztowych dla {ulica.Id}",
                        Miasto = ulica.Miasto
                    };
                    return result;
                }
                kodyPocztowe = kodyPocztoweMiasta;
            }

            searchLogger?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla ulicy");

            if (kodyPocztowe.Count == 0)
            {
                searchLogger?.Log("Ulica nie ma przypisanych kodów pocztowych");
                var result = _cityStrategy.Execute(request, foundMiasto, ulica, combinedBuildingNumber, searchLogger);

                // ✅ NOWE: Zapisz metodę dopasowania ulicy
                result.StreetMatchingMethod = wasStreetFuzzy ? MatchingMethod.Fuzzy : MatchingMethod.Strict;
                if (wasStreetFuzzy)
                {
                    result.AddMatchingDetail($"Ulica: fuzzy matching ('{request.Ulica}' → '{ulica.Nazwa1}')");
                }

                return result;
            }

            // Filtruj po numerze domu
            kodyPocztowe = FilterByBuildingNumber(kodyPocztowe, combinedBuildingNumber, ulica.Id, searchLogger);

            var finalResult = _resultFactory.CreateResult(
                kodyPocztowe, foundMiasto, ulica, combinedBuildingNumber, request.NumerMieszkania, searchLogger);

            // ✅ NOWE: Zapisz metodę dopasowania ulicy
            finalResult.StreetMatchingMethod = wasStreetFuzzy ? MatchingMethod.Fuzzy : MatchingMethod.Strict;
            if (wasStreetFuzzy)
            {
                finalResult.AddMatchingDetail($"Ulica: fuzzy matching ('{request.Ulica}' → '{ulica.Nazwa1}')");
            }

            return finalResult;
        }

        /// <summary>
        /// ✅ NOWA METODA: Znajduje ulice i zwraca informację czy użyto fuzzy matching
        /// </summary>
        private (List<(UlicaCached street, Miasto miasto)> streets, bool wasFuzzy) FindAllMatchingStreetsWithMethod(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string Prefix,
            string normalizedStreet,
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log($"Szukam WSZYSTKICH ulic pasujących do: '{request.Ulica}' -> znormalizowana: '{Prefix}/{normalizedStreet}'");

            var matchingStreets = new List<(UlicaCached street, Miasto miasto)>();
            bool wasFuzzy = false;

            // KROK 1: Dokładne dopasowanie
            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    foreach (var ulica in ulice)
                    {
                        if (_streetMatcher.IsMatch(ulica, normalizedStreet))
                        {
                            diagnostic?.Log($"  ✓ Znaleziono pasującą ulicę: ID:{ulica.Id} {_cache.GetOriginalStreetName(ulica)}");
                            matchingStreets.Add((ulica, miasto));
                        }
                    }
                }
            }

            if (matchingStreets.Count > 0)
            {
                diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic (strict matching)");
                return (matchingStreets, wasFuzzy: false);
            }

            // KROK 2: Fuzzy matching
            diagnostic?.Log($"Poszukiwanie mniej dokładne (fuzzy) miasto:{request.Miasto} ulica:{request.Ulica}");

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    var ulica = _streetMatcher.FindStreet(ulice, normalizedStreet);
                    if (ulica != null)
                    {
                        diagnostic?.Log($"  ✓ Znaleziono pasującą ulicę (fuzzy): ID:{ulica.Id} {_cache.GetOriginalStreetName(ulica)}");
                        matchingStreets.Add((ulica, miasto));
                        wasFuzzy = true;
                    }
                }
            }

            diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic (fuzzy matching)");
            return (matchingStreets, wasFuzzy);
        }

        /// <summary>
        /// 🆕 Próbuje rozwiązać niejednoznaczność wyboru ulicy
        /// </summary>
        private (UlicaCached street, Miasto miasto)? ResolveAmbiguousStreets(
            AddressSearchRequest request,
            List<(UlicaCached street, Miasto miasto)> matchingStreets,
            GeneralLogger? searchLogger)
        {
            // Jeśli na liście matchingStreets są ulice z więcej niż jednego miasta, poddajemy się
            var uniqueCities = matchingStreets.Select(m => m.miasto.Id).Distinct().Count();
            if (uniqueCities > 1)
            {
                searchLogger?.Log("  ✗ Niejednoznaczność: znaleziono ulice w więcej niż jednym mieście – nie rozstrzygamy.");
                return null;
            }

            var firstMiasto = matchingStreets[0].miasto;

            // ✅ POPRAWKA: Załaduj kody pocztowe z cache dla miasta
            if (!_cache.TryGetKodyPocztoweMiasta(firstMiasto.Id, out var kodyPocztowe))
            {
                kodyPocztowe = new List<KodPocztowy>();
            }

            // ✅ POPRAWKA: Przekształć UlicaCached na Ulica Z kodami pocztowymi
            var streets = matchingStreets
                .Select(m => new Ulica
                {
                    Id = m.street.Id,
                    MiastoId = m.street.MiastoId,
                    Cecha = m.street.Cecha,
                    Nazwa1 = m.street.Nazwa1,
                    Nazwa2 = m.street.Nazwa2,
                    Miasto = m.street.Miasto,
                    // ✅ Dodaj kody pocztowe dla tej ulicy
                    KodyPocztowe = kodyPocztowe
                        .Where(k => k.UlicaId == m.street.Id)
                        .ToList()
                })
                .ToList();

            (string? sPrefiks, string? sStreet) = UliceUtils.SplitStreetPrefix(request.Ulica);

            // Użyj ResolveAmbiguity
            var resolvedStreet = ResolveAmbiguity.ResolveStreetAmbiguity(
                streets,
                sPrefiks,
                sStreet,
                "", // nie znamy dzielnicy
                request.KodPocztowy,
                firstMiasto.Nazwa,
                _cache,
                searchLogger);

            if (resolvedStreet == null)
            {
                searchLogger?.Log("  ✗ Nie udało się automatycznie rozwiązać niejednoznaczności");
                return null;
            }

            // Znajdź odpowiadające miasto dla wybranej ulicy
            var matchedPair = matchingStreets.FirstOrDefault(m => m.street.Id == resolvedStreet.Id);

            if (matchedPair.street == null)
            {
                searchLogger?.Log("  ✗ Błąd: nie znaleziono pary (ulica, miasto)");
                return null;
            }

            searchLogger?.Log($"  ✓ Automatycznie wybrano: {UliceUtils.GetPelnaNazwa(resolvedStreet)}");
            return matchedPair;
        }

        /// <summary>
        /// Tworzy wynik z listą wszystkich niejednoznacznych dopasowań
        /// </summary>
        private AddressSearchResult CreateMultipleMatchesError(
            List<(UlicaCached street, Miasto miasto)> matchingStreets,
            List<Miasto> miasta,
            GeneralLogger? diagnostic)
        {
            // Pobierz kody pocztowe
            var firstMiasto = matchingStreets[0].miasto;
            if (!_cache.TryGetKodyPocztoweMiasta(firstMiasto.Id, out var postalCodes))
            {
                postalCodes = new List<KodPocztowy>();
            }

            var streets = matchingStreets.Select(m => m.street).ToList();
            var message = _ambiguityResolver.GetAmbiguityMessage(streets, postalCodes);

            diagnostic?.Log($" [A] ℹ️ {message}");

            var result = new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Message = message,
                Miasto = miasta.Count == 1 ? miasta[0] : null
            };
            result.AddDiagnostic($"Znaleziono {matchingStreets.Count} pasujących ulic");
            foreach (var (street, miasto) in matchingStreets.Take(10))
            {
                result.AddDiagnostic($"  • {_cache.GetOriginalStreetName(street)} w {miasto.Nazwa}");
            }
            return result;
        }

        private AddressSearchResult HandleStreetNotFound(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string Prefix,
            string normalizedStreet,
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log($"✗ Nie znaleziono ulicy '{request.Ulica}' w żadnej z miejscowości");

            // KROK 1: Sprawdź czy "ulica" to w rzeczywistości miejscowość
            if (Prefix == "")
            {
                var streetAsCityResult = TrySwapCityAndStreet(request, normalizedStreet, diagnostic);
                if (streetAsCityResult != null)
                {
                    return streetAsCityResult;
                }

            }
            // KROK 2: Sprawdź globalnie - czy ulica istnieje GDZIEKOLWIEK?
            var otherLocations = _cache.FindStreetGlobally(normalizedStreet);

            // ✅ ULICA NIE ISTNIEJE NIGDZIE → InvalidStreetName
            if (otherLocations.Count == 0)
            {
                diagnostic?.Log($"  ⚠️ UWAGA: Ulica '{request.Ulica}' NIE ISTNIEJE w całej bazie TERYT!");

                var result2 = new AddressSearchResult
                {
                    Status = AddressSearchStatus.InvalidStreetName,
                    Message = AddressSearchStatusInfo.GetMessage(
                        AddressSearchStatus.InvalidStreetName,
                        request.Ulica) + "/'" + normalizedStreet + "'",
                    Miasto = miasta.Count == 1 ? miasta[0] : null
                };
                result2.AddDiagnostic($"Szukana ulica: '{request.Ulica}'");
                result2.AddDiagnostic($"Znormalizowana: '{normalizedStreet}'");
                result2.AddDiagnostic("Ulica NIE istnieje w bazie TERYT");
                return result2;
            }

            // ✅ ULICA ISTNIEJE, ALE W INNYM MIEŚCIE → UlicaNotFound
            diagnostic?.Log($"  ℹ️ Ulica '{request.Ulica}' istnieje w {otherLocations.Count} innych miejscowościach");

            // KROK 3: Fuzzy matching
            var (suggestedStreet, suggestedMiasto) = FindSimilarStreet(request, miasta, diagnostic);

            if (suggestedStreet != null && suggestedMiasto != null)
            {
                diagnostic?.Log($"");
                diagnostic?.Log($"--- RETRY: Ponowne wyszukiwanie z sugerowaną ulicą ---");

                var foundUlica = new Ulica
                {
                    Id = suggestedStreet.Id,
                    MiastoId = suggestedStreet.MiastoId,
                    Cecha = suggestedStreet.Cecha,
                    Nazwa1 = suggestedStreet.Nazwa1,
                    Nazwa2 = suggestedStreet.Nazwa2,
                    Miasto = suggestedStreet.Miasto
                };

                diagnostic?.Log($"✓ Używam sugerowanej ulicy: {foundUlica.Cecha} {foundUlica.Nazwa1}");

                var combinedNum = request.NumerDomu ?? string.Empty;

                if (!_cache.TryGetKodyPocztoweMiasta(suggestedMiasto.Id, out var kodyPocztowe))
                {
                    return _cityStrategy.Execute(request, suggestedMiasto, foundUlica, combinedNum, diagnostic);
                }

                var filteredKody = _filters.FilterByStreet(kodyPocztowe, foundUlica.Id);
                filteredKody = FilterByBuildingNumber(filteredKody, combinedNum, foundUlica.Id, diagnostic);

                return _resultFactory.CreateResult(filteredKody, suggestedMiasto, foundUlica, combinedNum, request.NumerMieszkania, diagnostic);
            }

            // KROK 4: Zwróć błąd z komunikatem ze słownika
            var result = new AddressSearchResult
            {
                Status = AddressSearchStatus.UlicaNotFound,
                Message = AddressSearchStatusInfo.GetMessage(
                    AddressSearchStatus.UlicaNotFound,
                    $"'{request.Ulica}' w miejscowości '{request.Miasto}'"),
                Miasto = miasta.Count == 1 ? miasta[0] : null
            };
            result.AddDiagnostic($"Szukana ulica: '{request.Ulica}'");
            result.AddDiagnostic($"Miejscowość: '{request.Miasto}'");
            result.AddDiagnostic($"Ulica istnieje w {otherLocations.Count} innych miejscowościach");
            return result;
        }

        /// <summary>
        /// 🆕 Sprawdza czy podana "ulica" jest w rzeczywistości miejscowością
        /// Jeśli TAK - zamienia miejscami i ponawia wyszukiwanie BEZ ulicy
        /// </summary>
        private AddressSearchResult? TrySwapCityAndStreet(
            AddressSearchRequest request,
            string normalizedStreet,
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log($"{Environment.NewLine}🔄 Sprawdzam czy '{request.Ulica}' to miejscowość zamiast ulicy...");

            // Walidacja prefiksu
            var streetPrefixes = new[] { "os.", "os ", "al.", "al ", "pl.", "pl ", "ul.", "ul " };
            var ulicaLower = request?.Ulica?.ToLowerInvariant().TrimStart();

            if (streetPrefixes.Any(p => ulicaLower!=null && ulicaLower.StartsWith(p)))
            {
                diagnostic?.Log($"  ✗ '{request?.Ulica}' ma prefix osiedla/alei/placu - NIE zamieniaj na miejscowość");
                return null;
            }

            var citiesMatchingStreet = _cache.FindCitiesByName(normalizedStreet);

            if (citiesMatchingStreet.Count == 0)
            {
                var (similarCity, wasFuzzy) = CityUtils.FindSimilarCityWithMethod(_cache, normalizedStreet, request.KodPocztowy, diagnostic);

                if (similarCity != null)
                {
                    diagnostic?.Log($"  ✓ Znaleziono podobną miejscowość: '{similarCity.Nazwa}' (fuzzy: {wasFuzzy})");
                    citiesMatchingStreet = new List<Miasto> { similarCity };
                }
                else
                {
                    diagnostic?.Log($"  ✗ '{request.Ulica}' NIE jest miejscowością");
                    return null;
                }
            }

            diagnostic?.Log($"  ✓ Znaleziono {citiesMatchingStreet.Count} miejscowości o nazwie '{request.Ulica}'!");

            // Walidacja kodu pocztowego
            var Pasujace = new List<Miasto>();
            if (!string.IsNullOrWhiteSpace(request?.KodPocztowy))
            {
                foreach (var city in citiesMatchingStreet)
                {
                    var normalizedCode = UliceUtils.NormalizujKodPocztowy(request.KodPocztowy);

                    if (_cache.TryGetKodyPocztoweMiasta(city.Id, out var targetCityCodes))
                    {
                        var hasMatchingCode = targetCityCodes.Any(k => k.Kod == normalizedCode);

                        if (hasMatchingCode)
                        {
                            Pasujace.Add(city);
                            diagnostic?.Log($"  ✓ Kod pocztowy '{request.KodPocztowy}' pasuje do miejscowości '{city.Nazwa}'");
                        }
                    }
                }
            }

            if (Pasujace.Count == 0 || Pasujace.Count != 1)
            {
                diagnostic?.Log($"  Nie potrafię rozstrzygnąć, zwracam null");
                return null;
            }

            diagnostic?.Log($"  🔄 ZAMIANA: Miasto='{request.Miasto}' ↔ Ulica='{request.Ulica}'");

            var swappedRequest = new AddressSearchRequest
            {
                KodPocztowy = request.KodPocztowy,
                Miasto = request.Ulica,
                Ulica = "",
                NumerDomu = request.NumerDomu,
                NumerMieszkania = request.NumerMieszkania
            };

            diagnostic?.Log($"{Environment.NewLine}--- RETRY: Wyszukiwanie bez ulicy (bo '{request.Ulica}' to miejscowość) ---");

            var noStreetStrategy = new NoStreetSearchStrategy(_cache, _normalizer, _filters, _resultFactory);
            var result = noStreetStrategy.Execute(swappedRequest, new List<Miasto> { Pasujace[0] }, diagnostic);

            // ✅ NOWE: Oznacz że była zamiana
            if (result != null)
            {
                result.WasCityStreetSwapped = true;
                result.AddMatchingDetail($"Zamiana: ulica '{request.Ulica}' to w rzeczywistości miejscowość");
            }

            return result;
        }

        private (UlicaCached? street, Miasto? miasto) FindSimilarStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            GeneralLogger? diagnostic)
        {
            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    var similar = _streetMatcher.FindMostSimilarStreet(ulice, request.Ulica, maxDistance: 3);
                    if (similar != null)
                    {
                        diagnostic?.Log($"  💡 Znaleziono podobną ulicę: {similar.Cecha} {similar.Nazwa1}");
                        return (similar, miasto);
                    }
                }
            }

            return (null, null);
        }

        private List<KodPocztowy> FilterByBuildingNumber(
            List<KodPocztowy> filteredKody,
            string combinedBuildingNumber,
            int ulicaId,
            GeneralLogger? diagnostic)
        {
            if (string.IsNullOrWhiteSpace(combinedBuildingNumber))
                return filteredKody;

            var originalKody = filteredKody;
            var newFilteredKody = _filters.FilterByBuildingNumber(filteredKody, combinedBuildingNumber);
            diagnostic?.Log($"Po filtracji po numerze domu '{combinedBuildingNumber}': {newFilteredKody.Count} kodów (było: {filteredKody.Count()})");
            filteredKody = newFilteredKody;
            // Retry bez literki (np. 30A → 30)
            if (filteredKody.Count == 0 && System.Text.RegularExpressions.Regex.IsMatch(combinedBuildingNumber, @"\d+[A-Za-z]"))
            {
                var numberOnly = System.Text.RegularExpressions.Regex.Match(combinedBuildingNumber, @"^\d+").Value;

                if (!string.IsNullOrEmpty(numberOnly))
                {
                    diagnostic?.Log($"Retry bez literki: '{numberOnly}'");

                    if (_cache.TryGetKodyPocztoweMiasta(ulicaId, out var allKody))
                    {
                        var byStreet = _filters.FilterByStreet(allKody, ulicaId);
                        newFilteredKody = _filters.FilterByBuildingNumber(byStreet, numberOnly);
                        diagnostic?.Log($"Po filtracji po numerze '{numberOnly}': {newFilteredKody.Count} kodów");
                        filteredKody = newFilteredKody;
                    }
                }
            }
            if (filteredKody.Count > 0 || originalKody.Count == 0)
                return filteredKody;
            // Zwróć pierwszy z brzegu kod z wykrzyknikiem
            diagnostic?.Log($"Biorę pierwszy z brzegu kod, bo nie da się ustalić kodu pocztowego");
            var jednostkowa = new List<KodPocztowy>();
            var kod=ObjectCopier.ShallowCopy(originalKody[0]);
            if (originalKody.Count > 1) kod.Kod = $"!{kod.Kod}";
            jednostkowa.Add(kod);
            return jednostkowa;
        }
    }
}
