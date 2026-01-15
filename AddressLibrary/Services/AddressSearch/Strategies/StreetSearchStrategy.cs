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

        public StreetSearchStrategy(
            AddressSearchCache cache,
            TextNormalizer normalizer,
            StreetMatcher streetMatcher,
            PostalCodeFilters filters,
            CityPostalCodeStrategy cityStrategy,
            SearchResultFactory resultFactory)
        {
            _cache = cache;
            _normalizer = normalizer;
            _streetMatcher = streetMatcher;
            _filters = filters;
            _cityStrategy = cityStrategy;
            _resultFactory = resultFactory;
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
            diagnostic?.Log($"Działa nowa biblioteka");
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

            // 🆕 KROK 2: Jeśli jest WIĘCEJ NIŻ JEDNA ulica - zwróć błąd z listą
            if (matchingStreets.Count > 1)
            {
                diagnostic?.Log($"⚠ Znaleziono {matchingStreets.Count} pasujących ulic - niejednoznaczność!");
                
                var streetNames = matchingStreets
                    .Select(s => $"{s.street.Cecha} {s.street.Nazwa1}".Trim())
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                var streetList = string.Join(", ", streetNames);
                var message = $"Znaleziono wiele dopasowań ({matchingStreets.Count}): {streetList}";

                diagnostic?.Log($"  Lista ulic: {streetList}");

                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.MultipleMatches,
                    Message = message,
                    Miasto = matchingStreets.Count == 1 ? matchingStreets[0].miasto : null,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // 🆕 KROK 3: Dokładnie jedna ulica - kontynuuj normalnie
            var (foundUlica, foundMiasto) = matchingStreets[0];
            diagnostic?.Log($"✓ Znaleziono dokładnie jedną ulicę: {foundUlica.Cecha} {foundUlica.Nazwa1}");

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

        private bool IsCityAndStreetIdentical(AddressSearchRequest request, DiagnosticLogger? diagnostic)
        {
            if (string.IsNullOrWhiteSpace(request.Ulica) || string.IsNullOrWhiteSpace(request.Miasto))
                return false;

            var miejscNorm = _normalizer.Normalize(request.Miasto);
            var ulicaNorm = _normalizer.Normalize(request.Ulica);

            return miejscNorm == ulicaNorm;
        }

        /// <summary>
        /// 🆕 Znajduje WSZYSTKIE ulice pasujące do wyszukiwanej nazwy we WSZYSTKICH miastach
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

                    // ✅ KROK 1: Szukaj DOKŁADNEGO dopasowania
                    var exactMatch = _streetMatcher.FindStreetExact(ulice, request.Ulica);
                    if (exactMatch != null)
                    {
                        diagnostic?.Log($"  ✓ Dokładne dopasowanie: {exactMatch.Cecha} {exactMatch.Nazwa1}");
                        matchingStreets.Add((exactMatch, miasto));
                    }

                    // ✅ KROK 2: Jeśli nie znaleziono dokładnego, szukaj PARTIAL (może być WIELE!)
                    if (exactMatch == null)
                    {
                        diagnostic?.Log($"  ⚠️ Brak dokładnego dopasowania, szukam partial matching...");
                        
                        var partialMatches = _streetMatcher.FindAllStreets(ulice, request.Ulica);
                        
                        if (partialMatches.Count > 0)
                        {
                            diagnostic?.Log($"  ✓ Znaleziono {partialMatches.Count} częściowych dopasowań:");
                            foreach (var match in partialMatches)
                            {
                                diagnostic?.Log($"    - {match.Cecha} {match.Nazwa1}");
                                matchingStreets.Add((match, miasto));
                            }
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

            // Fuzzy matching
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

            // Brak podobnej ulicy
            var otherLocations = _cache.FindStreetGlobally(normalizedStreet);
            string errorMessage;

            if (otherLocations.Any())
            {
                errorMessage = $"Nie znaleziono ulicy '{request.Ulica}' w miejscowości {request.Miasto}";

                if (diagnostic != null)
                {
                    diagnostic.Log($"  ℹ️ UWAGA: Ulica '{request.Ulica}' istnieje w {otherLocations.Count} innych miejscowościach:");
                    foreach (var loc in otherLocations)
                    {
                        diagnostic.Log($"    - {loc.MiastoNazwa}: {loc.UlicaNazwa}");
                    }
                    diagnostic.Log($"  💡 Możliwe że podana miejscowość jest nieprawidłowa");
                }
            }
            else
            {
                errorMessage = $"Błędna nazwa ulicy '{request.Ulica}'";

                if (diagnostic != null)
                {
                    diagnostic.Log($"  ⚠️ UWAGA: Ulica '{request.Ulica}' NIE ISTNIEJE w całej bazie TERYT!");
                    diagnostic.Log($"  💡 Prawdopodobnie błędna nazwa ulicy w danych źródłowych");
                }
            }

            return new AddressSearchResult
            {
                Status = AddressSearchStatus.UlicaNotFound,
                Message = errorMessage,
                Miasto = miasta.Count == 1 ? miasta[0] : null,
                DiagnosticInfo = diagnostic?.GetLog()
            };
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