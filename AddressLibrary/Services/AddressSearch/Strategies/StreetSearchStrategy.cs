// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch.Filters;

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
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Szukanie z ulicą ---");

            // Sprawdź czy miejscowość == ulica (duplikat)
            if (IsCityAndStreetIdentical(request, diagnostic))
            {
                diagnostic?.Log($"⚠ Miejscowość i ulica są identyczne ('{request.Miasto}'), usuwam ulicę - szukam bez ulicy");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Miejscowość i ulica nie mogą być identyczne",
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // Normalizuj ulicę i wyciągnij numer
            var (normalizedStreet, extractedNumber) = _normalizer.NormalizeStreetWithNumber(request.Ulica);
            diagnostic?.Log($"Normalizacja ulicy: '{request.Ulica}' -> '{normalizedStreet}'");

            if (!string.IsNullOrEmpty(extractedNumber))
            {
                diagnostic?.Log($"Wyciągnięto numer z ulicy: '{extractedNumber}'");
            }

            var combinedBuildingNumber = CombineNumbers(extractedNumber, request.NumerDomu);
            diagnostic?.Log($"Połączony numer budynku: '{combinedBuildingNumber}'");

            // 🆕 KROK 1: Znajdź WSZYSTKIE pasujące ulice w WSZYSTKICH miastach
            var matchingStreets = FindAllMatchingStreets(request, miasta, normalizedStreet, diagnostic);

            if (matchingStreets.Count == 0)
            {
                return HandleStreetNotFound(request, miasta, normalizedStreet, diagnostic);
            }

            // 🆕 KROK 2: Jeśli jest WIĘCEJ NIŻ JEDNA ulica - użyj AmbiguousStreetResolver
            if (matchingStreets.Count > 1)
            {
                diagnostic?.Log($"⚠ Znaleziono {matchingStreets.Count} pasujących ulic - próba rozwiązania niejednoznaczności");

                var resolvedStreet = ResolveAmbiguousStreets(request, matchingStreets, diagnostic);

                if (resolvedStreet == null)
                {
                    // Nie udało się rozwiązać - zwróć błąd z listą wszystkich dopasowań
                    return CreateMultipleMatchesError(matchingStreets, miasta, diagnostic);
                }

                // ✅ Udało się rozwiązać niejednoznaczność - użyj wybranej ulicy
                matchingStreets = new List<(UlicaCached street, Miasto miasto)> { resolvedStreet.Value };
                diagnostic?.Log($"✓ Rozwiązano niejednoznaczność: {_cache.GetOriginalStreetName(resolvedStreet.Value.street)}");
            }

            // 🆕 KROK 3: Dokładnie jedna ulica - kontynuuj normalnie
            var (foundUlica, foundMiasto) = matchingStreets[0];
            diagnostic?.Log($"✓ Znaleziono dokładnie jedną ulicę: {_cache.GetOriginalStreetName(foundUlica)}");

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
            if (!_cache.TryGetKodyPocztowe(foundMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {foundMiasto.Id}");
                return _cityStrategy.Execute(request, foundMiasto, ulica, combinedBuildingNumber, diagnostic);
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // Filtruj po ulicy
            var filteredKody = _filters.FilterByStreet(kodyPocztowe, ulica.Id);
            diagnostic?.Log($"Po filtracji po ulicy (ID: {ulica.Id}): {filteredKody.Count} kodów");

            if (filteredKody.Count == 0)
            {
                diagnostic?.Log("Ulica nie ma przypisanych kodów pocztowych");
                return _cityStrategy.Execute(request, foundMiasto, ulica, combinedBuildingNumber, diagnostic);
            }

            // Filtruj po numerze domu
            filteredKody = FilterByBuildingNumber(filteredKody, combinedBuildingNumber, ulica.Id, diagnostic);

            return _resultFactory.CreateResult(filteredKody, foundMiasto, ulica, combinedBuildingNumber, request.NumerMieszkania, diagnostic);
        }

        /// <summary>
        /// 🆕 Próbuje rozwiązać niejednoznaczność wyboru ulicy
        /// </summary>
        private (UlicaCached street, Miasto miasto)? ResolveAmbiguousStreets(
            AddressSearchRequest request,
            List<(UlicaCached street, Miasto miasto)> matchingStreets,
            DiagnosticLogger? diagnostic)
        {
            // Wyciągnij tylko listę ulic (bez miast)
            var streets = matchingStreets.Select(m => m.street).ToList();

            // Pobierz kody pocztowe dla pierwszego miasta (zakładamy że wszystkie miasta mają te same ulice)
            var firstMiasto = matchingStreets[0].miasto;
            if (!_cache.TryGetKodyPocztowe(firstMiasto.Id, out var postalCodes))
            {
                diagnostic?.Log("  ✗ Brak kodów pocztowych dla miejscowości - nie można rozwiązać po kodzie");
                postalCodes = new List<KodPocztowy>();
            }

            // Użyj AmbiguousStreetResolver
            var resolvedStreet = _ambiguityResolver.ResolveAmbiguity(
                streets,
                request.Ulica,
                request.KodPocztowy,
                postalCodes);

            if (resolvedStreet == null)
            {
                diagnostic?.Log("  ✗ Nie udało się automatycznie rozwiązać niejednoznaczności");
                return null;
            }

            // Znajdź odpowiadające miasto dla wybranej ulicy
            var matchedPair = matchingStreets.FirstOrDefault(m => m.street.Id == resolvedStreet.Id);
            
            if (matchedPair.street == null)
            {
                diagnostic?.Log("  ✗ Błąd: nie znaleziono pary (ulica, miasto)");
                return null;
            }

            diagnostic?.Log($"  ✓ Automatycznie wybrano: {_cache.GetOriginalStreetName(resolvedStreet)}");
            return matchedPair;
        }

        /// <summary>
        /// Tworzy wynik z listą wszystkich niejednoznacznych dopasowań
        /// </summary>
        private AddressSearchResult CreateMultipleMatchesError(
            List<(UlicaCached street, Miasto miasto)> matchingStreets,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            // Pobierz kody pocztowe
            var firstMiasto = matchingStreets[0].miasto;
            if (!_cache.TryGetKodyPocztowe(firstMiasto.Id, out var postalCodes))
            {
                postalCodes = new List<KodPocztowy>();
            }

            var streets = matchingStreets.Select(m => m.street).ToList();
            var message = _ambiguityResolver.GetAmbiguityMessage(streets, postalCodes);

            diagnostic?.Log($"  ℹ️ {message}");

            return new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Message = message,
                Miasto = miasta.Count == 1 ? miasta[0] : null,
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

        private bool IsCityAndStreetIdentical(AddressSearchRequest request, DiagnosticLogger? diagnostic)
        {
            if (string.IsNullOrWhiteSpace(request.Ulica) || string.IsNullOrWhiteSpace(request.Miasto))
                return false;

            var miejscNorm = _normalizer.Normalize(request.Miasto);
            var ulicaNorm = _normalizer.Normalize(request.Ulica);

            return miejscNorm == ulicaNorm;
        }

        /// <summary>
        /// 🆕 Znajduje WSZYSTKIE ulice pasujące do wyszukiwanego nazwy we WSZYSTKICH miastach
        /// </summary>
        private List<(UlicaCached street, Miasto miasto)> FindAllMatchingStreets(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string normalizedStreet,
            DiagnosticLogger? diagnostic)
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

            diagnostic?.Log($"Łącznie znaleziono {matchingStreets.Count} pasujących ulic");
            return matchingStreets;
        }

        private AddressSearchResult HandleStreetNotFound(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string normalizedStreet,
            DiagnosticLogger? diagnostic)
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
                
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.InvalidStreetName,
                    Message = AddressSearchStatusInfo.GetMessage(
                        AddressSearchStatus.InvalidStreetName, 
                        request.Ulica), // ✅ "Błędna nazwa ulicy 'XYZ'"
                    Miasto = miasta.Count == 1 ? miasta[0] : null,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // ✅ ULICA ISTNIEJE, ALE W INNYM MIEŚCIE → UlicaNotFound
            diagnostic?.Log($"  ℹ️ Ulica '{request.Ulica}' istnieje w {otherLocations.Count} innych miejscowościach");

            // KROK 3: Fuzzy matching
            var (suggestedStreet, suggestedMiasto) = FindSimilarStreet(request, miasta, diagnostic);

            if (suggestedStreet != null && suggestedMiasto != null)
            {
                diagnostic?.Log($"\n--- RETRY: Ponowne wyszukiwanie z sugerowaną ulicą ---");

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

                if (!_cache.TryGetKodyPocztowe(suggestedMiasto.Id, out var kodyPocztowe))
                {
                    return _cityStrategy.Execute(request, suggestedMiasto, foundUlica, combinedNum, diagnostic);
                }

                var filteredKody = _filters.FilterByStreet(kodyPocztowe, foundUlica.Id);
                filteredKody = FilterByBuildingNumber(filteredKody, combinedNum, foundUlica.Id, diagnostic);

                return _resultFactory.CreateResult(filteredKody, suggestedMiasto, foundUlica, combinedNum, request.NumerMieszkania, diagnostic);
            }

            // KROK 4: Zwróć błąd z komunikatem ze słownika
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.UlicaNotFound,
                Message = AddressSearchStatusInfo.GetMessage(
                    AddressSearchStatus.UlicaNotFound, 
                    $"{request.Ulica} w miejscowości {request.Miasto}"), // ✅ "Nie znaleziono ulicy 'XYZ w miejscowości ABC'"
                Miasto = miasta.Count == 1 ? miasta[0] : null,
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

        /// <summary>
        /// 🆕 Sprawdza czy podana "ulica" jest w rzeczywistości miejscowością
        /// Jeśli TAK - zamienia miejscami i ponawia wyszukiwanie BEZ ulicy
        /// </summary>
        private AddressSearchResult? TrySwapCityAndStreet(
            AddressSearchRequest request,
            string normalizedStreet,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log($"\n🔄 Sprawdzam czy '{request.Ulica}' to miejscowość zamiast ulicy...");

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

            // Wybierz pierwszą miejscowość
            var targetCity = citiesMatchingStreet.FirstOrDefault();

            if (targetCity == null)
            {
                return null;
            }

            // ✅ WALIDACJA 2: Jeśli podano kod pocztowy, sprawdź czy pasuje do nowej miejscowości
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                var normalizedCode = _normalizer.NormalizePostalCode(request.KodPocztowy);

                if (_cache.TryGetKodyPocztowe(targetCity.Id, out var targetCityCodes))
                {
                    var hasMatchingCode = targetCityCodes.Any(k => k.Kod == normalizedCode);

                    if (!hasMatchingCode)
                    {
                        diagnostic?.Log($"  ✗ Kod pocztowy '{request.KodPocztowy}' NIE pasuje do miejscowości '{targetCity.Nazwa}' - NIE zamieniaj!");
                        return null;
                    }

                    diagnostic?.Log($"  ✓ Kod pocztowy '{request.KodPocztowy}' pasuje do miejscowości '{targetCity.Nazwa}'");
                }
            }

            diagnostic?.Log($"  🔄 ZAMIANA: Miasto='{request.Miasto}' ↔ Ulica='{request.Ulica}'");
            diagnostic?.Log($"  ➡️ Nowe wyszukiwanie: Miasto='{request.Ulica}' (bez ulicy)");

            // Utwórz nowe zapytanie: Miasto = stara "ulica", bez ulicy
            var swappedRequest = new AddressSearchRequest
            {
                KodPocztowy = request.KodPocztowy,
                Miasto = request.Ulica,  // 🔄 Zamiana!
                Ulica = null,             // 🔄 Usuń ulicę
                NumerDomu = request.NumerDomu,
                NumerMieszkania = request.NumerMieszkania
            };

            diagnostic?.Log($"\n--- RETRY: Wyszukiwanie bez ulicy (bo '{request.Ulica}' to miejscowość) ---");

            // Wyszukaj ponownie BEZ ulicy
            var noStreetStrategy = new NoStreetSearchStrategy(_cache, _normalizer, _filters, _resultFactory);
            return noStreetStrategy.Execute(swappedRequest, new List<Miasto> { targetCity }, diagnostic);
        }

        private (UlicaCached? street, Miasto? miasto) FindSimilarStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
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
            DiagnosticLogger? diagnostic)
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

                    if (_cache.TryGetKodyPocztowe(ulicaId, out var allKody))
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
