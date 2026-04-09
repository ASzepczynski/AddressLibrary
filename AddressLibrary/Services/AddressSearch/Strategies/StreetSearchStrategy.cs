// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Utils;
using Azure.Core;

namespace AddressLibrary.Services.AddressSearch.Strategies
{
    /// <summary>
    /// Strategia wyszukiwania adresu z podaną ulicą
    /// </summary>
    public class StreetSearchStrategy
    {
        private readonly AddressSearchCache _cache;
        private readonly StreetMatcher _streetMatcher;
        private readonly PostalCodeFilters _filters;
        private readonly CityPostalCodeStrategy _cityStrategy;
        private readonly SearchResultFactory _resultFactory;
        private readonly AmbiguousStreetResolver _ambiguityResolver;

        public StreetSearchStrategy(
            AddressSearchCache cache,
            StreetMatcher streetMatcher,
            PostalCodeFilters filters,
            CityPostalCodeStrategy cityStrategy,
            SearchResultFactory resultFactory,
            AmbiguousStreetResolver ambiguityResolver)
        {
            _cache = cache;
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
            (var Prefix, var normalizedStreet) = CechyUlicUtils.SplitStreetPrefix(request.Ulica);

            (var Prefix2, var normalizedStreet2) = CechyUlicUtils.SplitStreetPrefix(normalizedStreet);
            if (Prefix2 != "")
            {
                // Tu chcemy zlikwidować konflikt prefiksów typu ul. Szosa
                Prefix = Prefix2;
                normalizedStreet = normalizedStreet2;
            }

            normalizedStreet = TextNormalizer.Normalize(normalizedStreet);
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
                CechaUlicy = foundUlica.CechaUlicy,
                Miasto = foundUlica.Miasto
            };

            // Znajdź kody pocztowe
            if (!_cache.TryGetKodyPocztoweMiasta(ulica.Miasto.Id, out var wszystkieKodyMiasta))
            {
                searchLogger?.Log($"✗ Brak kodów pocztowych dla miasta {request.Miasto}");
                return ZwrocBrakKoduPocztowego(request, ulica);
            }

            // Filtruj po ulicy
            var kodyPocztowe = wszystkieKodyMiasta.Where(k => k.UlicaId == ulica.Id).ToList();

            searchLogger?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla ulicy");

            if (kodyPocztowe.Count == 0)
            {
                // Czy miasto ma jeden kod?
                if (wszystkieKodyMiasta.Count != 1)
                {
                    searchLogger?.Log($"✗ Ulica nie ma kodów, a miasto ma {wszystkieKodyMiasta.Count} kodów");
                    return ZwrocBrakKoduPocztowego(request, ulica);
                }
                
                searchLogger?.Log("Ulica nie ma przypisanych kodów pocztowych - używam kodu miasta");
                kodyPocztowe = wszystkieKodyMiasta;
            }

            searchLogger?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla ulicy");

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

        public AddressSearchResult ZwrocBrakKoduPocztowego(AddressSearchRequest request, Ulica ulica)
        {
            var result = new AddressSearchResult
            {
                Status = AddressSearchStatus.KodPocztowyNotFound,
                Message = $"Nie znaleziono kodów pocztowych dla {request.Miasto}/{request.Ulica}",
                Miasto = ulica.Miasto
            };
            if (request.KodPocztowy?.Length != 6)return result;
            // Uwaga, tu ustawiamy protezę kodu pocztowego
            var kod = new KodPocztowy()
            {
                Kod = $"!{request.KodPocztowy}",
                Miasto = ulica.Miasto,
                Ulica = ulica
            };
            result.Status = AddressSearchStatus.Success;
            result.KodPocztowy = kod;
            return result;
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    foreach (var ulica in ulice)
                    {
                        if (ulica.Postfiks == "zabia" || ulica.Postfiks=="Żabia")
                        {
                            int v = 11;
                        }

                        if (_streetMatcher.IsMatch(ulica, normalizedStreet))
                        {
                            diagnostic?.Log($"  ✓ Znaleziono pasującą ulicę: ID:{ulica.Id} {_cache.GetOriginalStreetName(ulica)}");
                            matchingStreets.Add((ulica, miasto));
                        }
                    }
                }
            }
            stopwatch.Stop();
            diagnostic?.Log($"⏱ Czas wykonania pętli foreach (strict matching): {stopwatch.ElapsedMilliseconds} ms");

            if (matchingStreets.Count > 0)
            {
                diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic (strict matching)");
                return (matchingStreets, wasFuzzy: false);
            }

            // 🆕 KROK 1.5: Jeśli nie znaleziono i ulica jest personalna (dwusłowowa), spróbuj tylko z nazwiskiem
            // Na razie nie działa

            // KROK 2: Fuzzy matching
            diagnostic?.Log($"Poszukiwanie mniej dokładne (fuzzy) miasto:{request.Miasto} ulica:{request.Ulica}");

            stopwatch.Restart();
            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    var ulica = _streetMatcher.FindStreet(ulice, normalizedStreet,out bool isFuzzy);
                    wasFuzzy = isFuzzy;
                    if (ulica != null)
                    {
                        diagnostic?.Log($"  ✓ Znaleziono pasującą ulicę (fuzzy): ID:{ulica.Id} {_cache.GetOriginalStreetName(ulica)}");
                        matchingStreets.Add((ulica, miasto));
                    }
                }
            }
            stopwatch.Stop();
            diagnostic?.Log($"⏱ Czas wykonania pętli foreach (fuzzy matching): {stopwatch.ElapsedMilliseconds} ms");

            diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic (fuzzy matching)");
            return (matchingStreets, wasFuzzy);
        }


        /// <summary>
        /// 🆕 Wyodrębnia nazwisko (ostatnie słowo) z nazwy ulicy personalnej
        /// </summary>
        private string GetLastName(string normalizedStreet)
        {
            var words = normalizedStreet.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length > 0 ? words[^1] : normalizedStreet;
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

            // ✅ POPRAWKA: Przekształć UlicaCached na Ulica z kodami pocztowymi
            var streets = matchingStreets
                .Select(m => new Ulica
                {
                    Id = m.street.Id,
                    MiastoId = m.street.MiastoId,
                    CechaUlicy = m.street.CechaUlicy,
                    Miasto = m.street.Miasto,
                })
                .ToList();

            (string? sPrefiks, string? sStreet) = CechyUlicUtils.SplitStreetPrefix(request.Ulica);

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

            // ✅ POPRAWKA: Znajdź odpowiadającą parę (UlicaCached, Miasto)
            var matchedPair = matchingStreets.FirstOrDefault(m => m.street.Id == resolvedStreet.Id);

            // ✅ POPRAWKA: Sprawdź czy znaleziono - dla ValueTuple trzeba sprawdzić czy street jest null
            if (matchedPair.street == null || matchedPair.miasto == null)
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

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // KROK 1: Sprawdź czy "ulica" to w rzeczywistości miejscowość
            var step1Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (Prefix == "")
            {
                var streetAsCityResult = TrySwapCityAndStreet(request, normalizedStreet, diagnostic);
                if (streetAsCityResult != null)
                {
                    step1Stopwatch.Stop();
                    diagnostic?.Log($"⏱ KROK 1 (TrySwapCityAndStreet): {step1Stopwatch.ElapsedMilliseconds} ms");
                    totalStopwatch.Stop();
                    diagnostic?.Log($"⏱ HandleStreetNotFound TOTAL: {totalStopwatch.ElapsedMilliseconds} ms");
                    return streetAsCityResult;
                }
            }
            step1Stopwatch.Stop();
            diagnostic?.Log($"⏱ KROK 1 (TrySwapCityAndStreet): {step1Stopwatch.ElapsedMilliseconds} ms");


            // KROK 2: Fuzzy matching
            var step3Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var (suggestedStreet, suggestedMiasto) = FindSimilarStreet(request, miasta, diagnostic);
            step3Stopwatch.Stop();
            diagnostic?.Log($"⏱ KROK 2 (FindSimilarStreet - fuzzy matching): {step3Stopwatch.ElapsedMilliseconds} ms");

            if (suggestedStreet != null && suggestedMiasto != null)
            {
                diagnostic?.Log($"");
                diagnostic?.Log($"--- RETRY: Ponowne wyszukiwanie z sugerowaną ulicą ---");

                var foundUlica = new Ulica
                {
                    Id = suggestedStreet.Id,
                    MiastoId = suggestedStreet.MiastoId,
                    CechaUlicy = suggestedStreet.CechaUlicy,
                    Miasto = suggestedStreet.Miasto
                };

                // ✅ POPRAWKA: Użyj GetDisplayName() zamiast Nazwa1
                diagnostic?.Log($"✓ Używam sugerowanej ulicy: {suggestedStreet.GetDisplayName()}");

                var combinedNum = request.NumerDomu ?? string.Empty;

                if (!_cache.TryGetKodyPocztoweMiasta(suggestedMiasto.Id, out var kodyPocztowe))
                {
                    var cityStrategyResult = _cityStrategy.Execute(request, suggestedMiasto, foundUlica, combinedNum, diagnostic);
                    
                    // ✅ NOWE: Oznacz że użyto fuzzy matching
                    cityStrategyResult.StreetMatchingMethod = MatchingMethod.Fuzzy;
                    // ✅ POPRAWKA: Użyj GetDisplayName() zamiast Nazwa1
                    cityStrategyResult.AddMatchingDetail($"Ulica: fuzzy matching ('{request.Ulica}' → '{suggestedStreet.GetDisplayName()}')");
                    
                    totalStopwatch.Stop();
                    diagnostic?.Log($"⏱ HandleStreetNotFound TOTAL: {totalStopwatch.ElapsedMilliseconds} ms");
                    return cityStrategyResult;
                }


                var filteredKody = kodyPocztowe;
                if (kodyPocztowe.Count > 1)
                {
                    // Sprawdzaj tylko gdy w mieście obowiązuje więcej niż jeden kod pocztowy
                    filteredKody = _filters.FilterByStreet(kodyPocztowe, foundUlica.Id);
                    filteredKody = FilterByBuildingNumber(filteredKody, combinedNum, foundUlica.Id, diagnostic);
                }
                
                var fuzzyResult = _resultFactory.CreateResult(filteredKody, suggestedMiasto, foundUlica, combinedNum, request.NumerMieszkania, diagnostic);
                
                // ✅ NOWE: Oznacz że użyto fuzzy matching
                fuzzyResult.StreetMatchingMethod = MatchingMethod.Fuzzy;
                // ✅ POPRAWKA: Użyj GetDisplayName() zamiast Nazwa1
                fuzzyResult.AddMatchingDetail($"Ulica: fuzzy matching ('{request.Ulica}' → '{suggestedStreet.GetDisplayName()}')");
                
                totalStopwatch.Stop();
                diagnostic?.Log($"⏱ HandleStreetNotFound TOTAL: {totalStopwatch.ElapsedMilliseconds} ms");
                return fuzzyResult;
            }

            // KROK 3: Sprawdź globalnie - czy ulica istnieje GDZIEKOLWIEK?
            var step2Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var otherLocations = _cache.FindStreetGlobally(normalizedStreet);
            step2Stopwatch.Stop();
            diagnostic?.Log($"⏱ KROK 3 (FindStreetGlobally): {step2Stopwatch.ElapsedMilliseconds} ms");

            // ✅ ULICA NIE ISTNIEJE NIGDZIE → InvalidStreetName
            if (otherLocations.Count == 0)
            {
                diagnostic?.Log($"  ⚠️ UWAGA: Ulica '{request.Ulica}' NIE ISTNIEJE w całej bazie TERYT!");

                var result2 = new AddressSearchResult
                {
                    Status = AddressSearchStatus.InvalidStreetName,
                    Message = AddressSearchStatusInfo.GetMessage(
                        AddressSearchStatus.InvalidStreetName,
                        $"'{request.Ulica}'"),
                    Miasto = miasta.Count == 1 ? miasta[0] : null
                };
                result2.AddDiagnostic($"Ulica '{request.Ulica}' nie istnieje w bazie TERYT");
                
                totalStopwatch.Stop();
                diagnostic?.Log($"⏱ HandleStreetNotFound TOTAL: {totalStopwatch.ElapsedMilliseconds} ms");
                return result2;
            }

            // ✅ ULICA ISTNIEJE, ALE W INNEJ MIEJSCOWOŚCI → UlicaNotFound
            diagnostic?.Log($"  ℹ️ Ulica '{request.Ulica}' istnieje, ale w innych miejscowościach:");
            foreach (var (miastoNazwa, ulicaNazwa) in otherLocations.Take(5))
            {
                diagnostic?.Log($"    • {ulicaNazwa} w {miastoNazwa}");
            }

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
            
            totalStopwatch.Stop();
            diagnostic?.Log($"⏱ HandleStreetNotFound TOTAL: {totalStopwatch.ElapsedMilliseconds} ms");
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

            if (streetPrefixes.Any(p => ulicaLower != null && ulicaLower.StartsWith(p)))
            {
                diagnostic?.Log($"  ✗ '{request?.Ulica}' ma prefix osiedla/alei/placu - NIE zamieniaj na miejscowość");
                return null;
            }

            var citiesMatchingStreet = _cache.FindCitiesByName(normalizedStreet);

            if (citiesMatchingStreet.Count == 0)
            {
                diagnostic?.Log($"  ✗ '{request.Ulica}' NIE jest miejscowością");
                return null;
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

            var noStreetStrategy = new NoStreetSearchStrategy(_cache, _filters, _resultFactory);
            var result = noStreetStrategy.Execute(swappedRequest, new List<Miasto> { Pasujace[0] }, diagnostic);

            // ✅ NOWE: Oznacz że była zamiana
            if (result != null)
            {
                result.WasCityStreetSwapped = true;
                result.AddMatchingDetail($"Zamiana: ulica '{request.Ulica}' to w rzeczywistości miejscowość");
            }

            return result;
        }

        /// <summary>
        /// 🆕 Znajduje podobną ulicę w podanych miastach (fuzzy matching)
        /// </summary>
        private (UlicaCached? street, Miasto? miasto) FindSimilarStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            GeneralLogger? diagnostic)
        {
            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    // ✅ POPRAWKA: Użyj FindStreet (która robi fuzzy matching z wagami)
                    var similar = _streetMatcher.FindStreet(ulice, request.Ulica,out bool wasFuzzy);
                    if (similar != null)
                    {
                        // ✅ POPRAWKA: Użyj GetDisplayName() zamiast Nazwa1
                        diagnostic?.Log($"  💡 Znaleziono podobną ulicę: {similar.GetDisplayName()}");
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

                    if (_cache.TryGetKodyPocztoweUlicy(ulicaId, out var allKody))
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
