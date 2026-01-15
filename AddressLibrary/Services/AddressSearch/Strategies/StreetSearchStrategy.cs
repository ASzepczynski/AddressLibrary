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
                // Tutaj trzeba wywołać NoStreetSearchStrategy - ale to tworzy cykliczną zależność
                // Więc zwracamy błąd i pozwalamy orchestratorowi to obsłużyć
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

            // Znajdź ulicę we wszystkich miejscowościach
            var (foundUlica, foundMiasto) = FindStreet(request, miasta, normalizedStreet, diagnostic);

            if (foundUlica == null || foundMiasto == null)
            {
                return HandleStreetNotFound(request, miasta, normalizedStreet, diagnostic);
            }

            // Znajdź kody pocztowe
            if (!_cache.TryGetKodyPocztowe(foundMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {foundMiasto.Id}");
                return _cityStrategy.Execute(request, foundMiasto, foundUlica, combinedBuildingNumber, diagnostic);
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // Filtruj po ulicy
            var filteredKody = _filters.FilterByStreet(kodyPocztowe, foundUlica.Id);
            diagnostic?.Log($"Po filtracji po ulicy (ID: {foundUlica.Id}): {filteredKody.Count} kodów");

            if (filteredKody.Count == 0)
            {
                diagnostic?.Log("Ulica nie ma przypisanych kodów pocztowych");
                return _cityStrategy.Execute(request, foundMiasto, foundUlica, combinedBuildingNumber, diagnostic);
            }

            // Filtruj po numerze domu
            filteredKody = FilterByBuildingNumber(filteredKody, combinedBuildingNumber, foundUlica.Id, diagnostic);

            return _resultFactory.CreateResult(filteredKody, foundMiasto, foundUlica, combinedBuildingNumber, request.NumerMieszkania, diagnostic);
        }

        private bool IsCityAndStreetIdentical(AddressSearchRequest request, DiagnosticLogger? diagnostic)
        {
            if (string.IsNullOrWhiteSpace(request.Ulica) || string.IsNullOrWhiteSpace(request.Miasto))
                return false;

            var miejscNorm = _normalizer.Normalize(request.Miasto);
            var ulicaNorm = _normalizer.Normalize(request.Ulica);

            return miejscNorm == ulicaNorm;
        }

        private (Ulica? ulica, Miasto? miasto) FindStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            string normalizedStreet,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log($"Szukam ulicy: '{request.Ulica}' -> znormalizowana: '{normalizedStreet}'");

            // Zbierz wszystkie miasta, które mają tę ulicę
            var miastaZUlica = new List<(Ulica ulica, Miasto miasto)>();

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    // ✅ KROK 1: Najpierw spróbuj dokładnego dopasowania
                    var foundCached = _streetMatcher.FindStreetExact(ulice, request.Ulica);

                    // ✅ KROK 2: Jeśli nie znaleziono, spróbuj z partial matching (dla patronów)
                    if (foundCached == null)
                    {
                        diagnostic?.Log($"  ⚠️ Dokładne dopasowanie nie powiodło się, próbuję partial matching...");
                        foundCached = _streetMatcher.FindStreet(ulice, request.Ulica);
                        
                        if (foundCached != null)
                        {
                            diagnostic?.Log($"  ✓ Znaleziono przez partial matching: {foundCached.Cecha} {foundCached.Nazwa1}");
                        }
                    }

                    if (foundCached != null)
                    {
                        var foundUlica = new Ulica
                        {
                            Id = foundCached.Id,
                            MiastoId = foundCached.MiastoId,
                            Cecha = foundCached.Cecha,
                            Nazwa1 = foundCached.Nazwa1,
                            Nazwa2 = foundCached.Nazwa2,
                            Miasto = foundCached.Miasto
                        };

                        diagnostic?.Log($"✓ Znaleziono ulicę: {foundCached.Cecha} {foundCached.Nazwa1} w {miasto.Nazwa} (ID: {foundCached.Id})");
                        miastaZUlica.Add((foundUlica, miasto));
                    }
                }
            }

            // Jeśli nie znaleziono ulicy w żadnym mieście
            if (miastaZUlica.Count == 0)
            {
                diagnostic?.Log("✗ Nie znaleziono ulicy w żadnym z miast");
                return (null, null);
            }

            // Jeśli jest tylko jedno miasto z tą ulicą - zwróć je
            if (miastaZUlica.Count == 1)
            {
                diagnostic?.Log($"✓ Ulica znaleziona w dokładnie jednym mieście: {miastaZUlica[0].miasto.Nazwa}");
                return (miastaZUlica[0].ulica, miastaZUlica[0].miasto);
            }

            // Wiele miast ma tę ulicę - próbuj zawęzić po kodzie pocztowym
            diagnostic?.Log($"⚠ Ulica znaleziona w {miastaZUlica.Count} miastach");

            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                diagnostic?.Log($"Próba zawężenia po kodzie pocztowym: {kodNorm}");

                foreach (var (ulica, miasto) in miastaZUlica)
                {
                    if (_cache.TryGetKodyPocztowe(miasto.Id, out var kodyPocztowe))
                    {
                        var byStreet = _filters.FilterByStreet(kodyPocztowe, ulica.Id);
                        if (byStreet.Any(k => k.Kod == kodNorm))
                        {
                            diagnostic?.Log($"✓ Wybrano miasto po kodzie pocztowym: {miasto.Nazwa} (woj. {miasto.Gmina?.Powiat?.Wojewodztwo?.Nazwa})");
                            return (ulica, miasto);
                        }
                    }
                }

                diagnostic?.Log($"✗ Kod pocztowy {kodNorm} nie pasuje do żadnego z {miastaZUlica.Count} miast z tą ulicą");
            }
            else
            {
                diagnostic?.Log("✗ Brak kodu pocztowego do zawężenia wyboru miasta");
            }

            // Nie można jednoznacznie określić miasta - zwróć błąd
            diagnostic?.Log($"✗ Niejednoznaczne miasto - ulica '{request.Ulica}' występuje w {miastaZUlica.Count} miejscowościach:");
            
            if (diagnostic != null)
            {
                foreach (var (_, miasto) in miastaZUlica)
                {
                    diagnostic.Log($"  - {miasto.Nazwa} (woj. {miasto.Gmina?.Powiat?.Wojewodztwo?.Nazwa}, powiat {miasto.Gmina?.Powiat?.Nazwa})");
                }
            }

            return (null, null);
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

            // NIE zwracamy miasta[0] - zwracamy null dla niejednoznaczności
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.UlicaNotFound,
                Message = errorMessage,
                Miasto = miasta.Count == 1 ? miasta[0] : null, // Tylko gdy jest jedno miasto
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
                    // 🔧 ZAOSTRZENIE: maxDistance = 1 (tylko 1 literka błędu)
                    // Przykład: "Łowiecka" vs "Łokietka" ma odległość 2 → NIE DOPASUJE
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