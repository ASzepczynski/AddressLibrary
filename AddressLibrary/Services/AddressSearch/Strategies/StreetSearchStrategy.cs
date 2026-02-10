// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Models;
using AddressLibrary.Logging;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader;
using System.Collections.Generic;

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

            // Normalizuj ulicę i wyciągnij numer
            var normalizedStreet = _normalizer.Normalize(request.Ulica);
            searchLogger?.Log($"Normalizacja ulicy: '{request.Ulica}' -> '{normalizedStreet}'");

            var combinedBuildingNumber = request.NumerDomu;

            // 🆕 KROK 1: Znajdź WSZYSTKIE pasujące ulice w WSZYSTKICH miastach o podanej nazwie
            var matchingStreets = FindAllMatchingStreets(request, miasta, normalizedStreet, searchLogger);

            if (matchingStreets.Count == 0)
            {
                return HandleStreetNotFound(request, miasta, normalizedStreet, searchLogger);
            }

            // 🆕 KROK 2: Jeśli jest WIĘCEJ NIŻ JEDNA ulica - użyj AmbiguousStreetResolver
            if (matchingStreets.Count > 1)
            {
                searchLogger?.Log($"⚠ Znaleziono {matchingStreets.Count} pasujących ulic - próba rozwiązania niejednoznaczności");

                var resolvedStreet = ResolveAmbiguousStreets(request, matchingStreets, searchLogger);

                if (resolvedStreet == null)
                {
                    // Nie udało się rozwiązać - zwróć błąd z listą wszystkich dopasowań
                    return CreateMultipleMatchesError(matchingStreets, miasta, searchLogger);
                }

                // ✅ Udało się rozwiązać niejednoznaczność - użyj wybranej ulicy
                matchingStreets = new List<(UlicaCached street, Miasto miasto)> { resolvedStreet.Value };
                searchLogger?.Log($"✓ Rozwiązano niejednoznaczność: {_cache.GetOriginalStreetName(resolvedStreet.Value.street)}");
            }

            // 🆕 KROK 3: Dokładnie jedna ulica - kontynuuj normalnie
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
            if (!_cache.TryGetKodyPocztoweMiasta(foundMiasto.Id, out var kodyPocztowe))
            {
                searchLogger?.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {foundMiasto.Id}");
                return _cityStrategy.Execute(request, foundMiasto, ulica, combinedBuildingNumber, searchLogger);
            }

            searchLogger?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // Filtruj po ulicy
            var filteredKody = _filters.FilterByStreet(kodyPocztowe, ulica.Id);
            searchLogger?.Log($"Po filtracji po ulicy (ID: {ulica.Id}): {filteredKody.Count} kodów");

            if (filteredKody.Count == 0)
            {
                searchLogger?.Log("Ulica nie ma przypisanych kodów pocztowych");
                return _cityStrategy.Execute(request, foundMiasto, ulica, combinedBuildingNumber, searchLogger);
            }

            // Filtruj po numerze domu
            filteredKody = FilterByBuildingNumber(filteredKody, combinedBuildingNumber, ulica.Id, searchLogger);

            return _resultFactory.CreateResult(filteredKody, foundMiasto, ulica, combinedBuildingNumber, request.NumerMieszkania, searchLogger);
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

            (string sPrefiks, string sStreet) = UliceUtils.SplitStreetAndPrefix(request.Ulica);

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



        /// <summary>
        /// 🆕 Znajduje WSZYSTKIE ulice pasujące do wyszukiwanego nazwy we WSZYSTKICH miastach
        /// </summary>
        private List<(UlicaCached street, Miasto miasto)> FindAllMatchingStreets(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string normalizedStreet,
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log($"Szukam WSZYSTKICH ulic pasujących do: '{request.Ulica}' -> znormalizowana: '{normalizedStreet}'");

            var matchingStreets = new List<(UlicaCached street, Miasto miasto)>();

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    foreach (var ulica in ulice)
                    {

                        // ✅ Sprawdź dokładne dopasowanie
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
                diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic");
                return matchingStreets;
            }

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    var ulica = _streetMatcher.FindStreet(ulice, normalizedStreet);
                    if (ulica != null)
                    {
                        diagnostic?.Log($"  ✓ Znaleziono pasującą ulicę: ID:{ulica.Id} {_cache.GetOriginalStreetName(ulica)}");
                        matchingStreets.Add((ulica, miasto));
                    }
                }
            }
            diagnostic?.Log($"Włączenie fuzzy matching - łącznie znaleziono {matchingStreets.Count} pasujących ulic");
            return matchingStreets;
        }

        private AddressSearchResult HandleStreetNotFound(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string normalizedStreet,
            GeneralLogger? diagnostic)
        {
            diagnostic?.Log($"✗ Nie znaleziono ulicy '{request.Ulica}' w żadnej z miejscowości");

            // KROK 1: Sprawdź czy "ulica" to w rzeczywistości miejscowość
            var streetAsCityResult = TrySwapCityAndStreet(request, normalizedStreet, diagnostic);
            if (streetAsCityResult != null)
            {
                return streetAsCityResult;
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

            // ✅ WALIDACJA 1: Jeśli ulica ma prefix (os., al., pl., ul.), to NIE ZAMIENIAJ!
            var streetPrefixes = new[] { "os.", "os ", "al.", "al ", "pl.", "pl ", "ul.", "ul " };
            var ulicaLower = request.Ulica.ToLowerInvariant().TrimStart();

            if (streetPrefixes.Any(p => ulicaLower.StartsWith(p)))
            {
                diagnostic?.Log($"  ✗ '{request.Ulica}' ma prefix osiedla/alei/placu - NIE zamieniaj na miejscowość");
                return null;
            }

            // Znajdź miejscowość o nazwie jak "ulica"
            var citiesMatchingStreet = _cache.FindCitiesByName(normalizedStreet);

            if (citiesMatchingStreet.Count == 0)
            {
                diagnostic?.Log($"  ✗ '{request.Ulica}' NIE jest miejscowością");
                return null;
            }

            diagnostic?.Log($"  ✓ Znaleziono {citiesMatchingStreet.Count} miejscowości o nazwie '{request.Ulica}'!");


            var Pasujace = new List<Miasto>();
            // ✅ WALIDACJA 2: Jeśli podano kod pocztowy, sprawdź czy pasuje do nowej miejscowości
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
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
            if (Pasujace.Count() == 0)
            {
                diagnostic?.Log($" Ulica nie jest miastem, zwracam null");
                return null;
            }

            if (Pasujace.Count() != 1)
            {
                diagnostic?.Log($"  🔄 Istnieje wiele miast '{request.Ulica}' o kodzie Ulica='{request.KodPocztowy}'");
                diagnostic?.Log($"  Nie potrafię rozstrzygnąć, zwracam null");
                return null;
            }
            diagnostic?.Log($"  🔄 ZAMIANA: Miasto='{request.Miasto}' ↔ Ulica='{request.Ulica}'");
            diagnostic?.Log($"  ➡️ Nowe wyszukiwanie: Miasto='{request.Ulica}' (bez ulicy)");

            // Utwórz nowe zapytanie: Miasto = stara "ulica", bez ulicy
            var swappedRequest = new AddressSearchRequest
            {
                KodPocztowy = request.KodPocztowy,
                Miasto = request.Ulica,  // 🔄 Zamiana!
                Ulica = "",             // 🔄 Usuń ulicę
                NumerDomu = request.NumerDomu,
                NumerMieszkania = request.NumerMieszkania
            };

            diagnostic?.Log($"{Environment.NewLine}--- RETRY: Wyszukiwanie bez ulicy (bo '{request.Ulica}' to miejscowość) ---");

            // Wyszukaj ponownie BEZ ulicy
            var noStreetStrategy = new NoStreetSearchStrategy(_cache, _normalizer, _filters, _resultFactory);
            return noStreetStrategy.Execute(swappedRequest, new List<Miasto> { Pasujace[0] }, diagnostic);
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
                    var similar = _streetMatcher.FindMostSimilarStreet(ulice, request.Ulica, maxDistance: 1);
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

            var beforeFilter = filteredKody.Count;
            filteredKody = _filters.FilterByBuildingNumber(filteredKody, combinedBuildingNumber);
            diagnostic?.Log($"Po filtracji po numerze domu '{combinedBuildingNumber}': {filteredKody.Count} kodów (było: {beforeFilter})");

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
                        filteredKody = _filters.FilterByBuildingNumber(byStreet, numberOnly);
                        diagnostic?.Log($"Po filtracji po numerze '{numberOnly}': {filteredKody.Count} kodów");
                    }
                }
            }

            return filteredKody;
        }

        private string CombineNumbers(string? extractedNumber, string? providedNumber)
        {
            if (string.IsNullOrWhiteSpace(extractedNumber) && string.IsNullOrWhiteSpace(providedNumber))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(extractedNumber))
                return providedNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(providedNumber))
                return extractedNumber.Trim();

            return $"{extractedNumber.Trim()}/{providedNumber.Trim()}";
        }
    }
}
