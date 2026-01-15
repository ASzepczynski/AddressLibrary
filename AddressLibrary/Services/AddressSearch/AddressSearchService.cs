// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Główny serwis do wyszukiwania adresów używający słowników w pamięci
    /// </summary>
    public class AddressSearchService
    {
        private readonly AddressSearchCache _cache;
        private readonly TextNormalizer _normalizer;
        private readonly StreetMatcher _streetMatcher;
        private readonly BuildingNumberValidator _numberValidator;

        public AddressSearchService(AddressDbContext context)
        {
            _normalizer = new TextNormalizer();
            _cache = new AddressSearchCache(context, _normalizer);
            _streetMatcher = new StreetMatcher(_normalizer);
            _numberValidator = new BuildingNumberValidator();
        }

        /// <summary>
        /// Inicjalizuje słowniki z bazy danych
        /// </summary>
        public async Task InitializeAsync()
        {
            await _cache.InitializeAsync();
        }

        /// <summary>
        /// Wyszukuje adres na podstawie parametrów (z opcjonalnym logowaniem diagnostycznym)
        /// </summary>
        public async Task<AddressSearchResult> SearchAsync(
            AddressSearchRequest request,
            bool enableDiagnostics = false)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            // 🚀 OPTYMALIZACJA: DiagnosticLogger tylko gdy potrzebny
            DiagnosticLogger? diagnostic = enableDiagnostics ? new DiagnosticLogger() : null;

            // Walidacja
            if (string.IsNullOrWhiteSpace(request.Miasto))
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Nazwa miejscowości jest wymagana"
                };
            }

            // Krok 1: Znajdź miejscowość (lub miejscowości o tej nazwie)
            var miasta = FindAllMiasta(request.Miasto, diagnostic);
            if (miasta == null || miasta.Count == 0)
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.MiastoNotFound,
                    Message = $"Nie znaleziono miejscowości: {request.Miasto}",
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // Krok 2: Znajdź ulicę (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                return SearchWithStreet(request, miasta, diagnostic);
            }
            else
            {
                return SearchWithoutStreet(request, miasta, diagnostic);
            }
        }

        /// <summary>
        /// Masowe wyszukiwanie adresów (BEZ logowania diagnostycznego)
        /// </summary>
        public async Task<List<AddressSearchResult>> SearchBatchAsync(IEnumerable<AddressSearchRequest> requests)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            var results = new List<AddressSearchResult>();

            // 🚀 OPTYMALIZACJA: Wyłącz diagnostykę w trybie batch
            foreach (var request in requests)
            {
                var result = await SearchAsync(request, enableDiagnostics: false);
                results.Add(result);
            }

            return results;
        }

        #region Private Methods

        /// <summary>
        /// Znajduje wszystkie miejscowości o podanej nazwie
        /// </summary>
        private List<Miasto>? FindAllMiasta(string miastoName, DiagnosticLogger? diagnostic)
        {
            var miastoNorm = _normalizer.Normalize(miastoName);
            diagnostic?.Log($"Znormalizowana miejscowość: '{miastoName}' -> '{miastoNorm}'");

            if (!_cache.TryGetMiasta(miastoNorm, out var miasta))
            {
                return null;
            }

            diagnostic?.Log($"Znaleziono {miasta.Count} miejscowości o nazwie '{miastoNorm}'");
            return miasta;
        }

        /// <summary>
        /// Wyszukiwanie z podaną ulicą - ZOPTYMALIZOWANA WERSJA
        /// </summary>
        private AddressSearchResult SearchWithStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Szukanie z ulicą ---");

            // 🆕 SPRAWDŹ: Czy miejscowość == ulica (duplikat)
            if (!string.IsNullOrWhiteSpace(request.Ulica) &&
                !string.IsNullOrWhiteSpace(request.Miasto))
            {
                var miejscNorm = _normalizer.Normalize(request.Miasto);
                var ulicaNorm = _normalizer.Normalize(request.Ulica);

                if (miejscNorm == ulicaNorm)
                {
                    diagnostic?.Log($"⚠ Miejscowość i ulica są identyczne ('{request.Miasto}'), usuwam ulicę - szukam bez ulicy");
                    return SearchWithoutStreet(request, miasta, diagnostic);
                }
            }

            // Normalizuj ulicę i wyciągnij numer (jeśli jest)
            var (normalizedStreet, extractedNumber) = _normalizer.NormalizeStreetWithNumber(request.Ulica);
            diagnostic?.Log($"Działa nowa biblioteka");
            diagnostic?.Log($"Normalizacja ulicy: '{request.Ulica}' -> '{normalizedStreet}'");

            if (!string.IsNullOrEmpty(extractedNumber))
            {
                diagnostic?.Log($"Wyciągnięto numer z ulicy: '{extractedNumber}'");
            }

            // Skonkatenuj numery: wyciągnięty z ulicy + podany w żądaniu
            var combinedBuildingNumber = CombineNumbers(extractedNumber, request.NumerDomu);
            diagnostic?.Log($"Połączony numer budynku: '{combinedBuildingNumber}'");

            // 🚀 OPTYMALIZACJA: Szukaj ulicy we wszystkich miejscowościach
            Ulica? foundUlica = null;
            Miasto? foundMiasto = null;

            diagnostic?.Log($"Szukam ulicy: '{request.Ulica}' -> znormalizowana: '{normalizedStreet}'");

            foreach (var miasto in miasta)
            {
                if (_cache.TryGetUlice(miasto.Id, out var ulice))
                {
                    diagnostic?.Log($"Sprawdzam miejscowość: {miasto.Nazwa} (ID: {miasto.Id}), ulic: {ulice.Count}");

                    // 🔍 DIAGNOSTYKA: Pokaż ulice zawierające szukany termin
                    if (diagnostic != null && (normalizedStreet.Contains("slomian") || normalizedStreet.Contains("słomian")))
                    {
                        diagnostic.Log($"  🔍 DEBUG MiastoId dla Krakowa: {miasto.Id}");
                        diagnostic.Log($"  🔍 Wszystkie ulice '*slomian* lub '*słomian*' w CAŁYM cache (niezależnie od miejscowości):");
                        
                        // Przeszukaj WSZYSTKIE miejscowości w cache
                        foreach (var miejsc in miasta)
                        {
                            if (_cache.TryGetUlice(miejsc.Id, out var uliceDebug))
                            {
                                var slomUlice = uliceDebug.Where(u => u.NormalizedNazwa1.Contains("slom")).ToList();
                                if (slomUlice.Any())
                                {
                                    foreach (var u in slomUlice)
                                        diagnostic.Log($"    MiejscId:{u.MiastoId} | '{u.NormalizedNazwa1}' | Oryg: '{u.Cecha} {u.Nazwa1}'");
                                }
                            }
                        }
                    }

                    // 🔧 Użyj FindStreetExact dla precyzyjnego dopasowania
                    var foundCached = _streetMatcher.FindStreetExact(ulice, request.Ulica);

                    if (foundCached != null)
                    {
                        // Znaleziono - skonwertuj z powrotem na Ulica
                        foundUlica = new Ulica
                        {
                            Id = foundCached.Id,
                            MiastoId = foundCached.MiastoId,
                            Cecha = foundCached.Cecha,
                            Nazwa1 = foundCached.Nazwa1,
                            Nazwa2 = foundCached.Nazwa2,
                            Miasto = foundCached.Miasto
                        };
                        foundMiasto = miasto;
                        diagnostic?.Log($"✓ Znaleziono ulicę: {foundCached.Cecha} {foundCached.Nazwa1} w {miasto.Nazwa} (ID: {foundCached.Id})");
                        break;
                    }
                }
            }

            if (foundUlica == null || foundMiasto == null)
            {
                diagnostic?.Log($"✗ Nie znaleziono ulicy '{request.Ulica}' w żadnej z miejscowości");

                // 🆕 FUZZY MATCHING: Spróbuj znaleźć najbardziej podobną ulicę
                UlicaCached? suggestedStreet = null;
                Miasto? suggestedMiasto = null;

                foreach (var miasto in miasta)
                {
                    if (_cache.TryGetUlice(miasto.Id, out var ulice))
                    {
                        var similar = _streetMatcher.FindMostSimilarStreet(ulice, request.Ulica, maxDistance: 3);
                        if (similar != null)
                        {
                            suggestedStreet = similar;
                            suggestedMiasto = miasto;
                            diagnostic?.Log($"  💡 Znaleziono podobną ulicę: {similar.Cecha} {similar.Nazwa1}");
                            break;
                        }
                    }
                }

                // Jeśli znaleziono podobną ulicę, spróbuj ponownie z nią
                if (suggestedStreet != null && suggestedMiasto != null)
                {
                    diagnostic?.Log($"\n--- RETRY: Ponowne wyszukiwanie z sugerowaną ulicą ---");
                    
                    // Skonwertuj z UlicaCached na Ulica
                    foundUlica = new Ulica
                    {
                        Id = suggestedStreet.Id,
                        MiastoId = suggestedStreet.MiastoId,
                        Cecha = suggestedStreet.Cecha,
                        Nazwa1 = suggestedStreet.Nazwa1,
                        Nazwa2 = suggestedStreet.Nazwa2,
                        Miasto = suggestedStreet.Miasto
                    };
                    foundMiasto = suggestedMiasto;
                    
                    diagnostic?.Log($"✓ Używam sugerowanej ulicy: {foundUlica.Cecha} {foundUlica.Nazwa1}");
                    
                    // KONTYNUUJ normalny przepływ (nie return!)
                }
                else
                {
                    // Brak podobnej ulicy - sprawdź czy ulica istnieje gdzie indziej
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
                        Miasto = miasta[0],
                        NormalizedBuildingNumber = combinedBuildingNumber,
                        NormalizedApartmentNumber = request.NumerMieszkania,
                        DiagnosticInfo = diagnostic?.GetLog()
                    };
                }
            }

            // 🆕 Jeśli dotarliśmy tutaj, to mamy foundUlica (albo oryginalną, albo sugerowaną)
            // Kontynuuj normalny przepływ wyszukiwania kodów pocztowych:

            // Sprawdź kody pocztowe dla znalezionej ulicy
            if (!_cache.TryGetKodyPocztowe(foundMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {foundMiasto.Id}");
                return TryReturnCityPostalCode(request, foundMiasto, foundUlica, combinedBuildingNumber, diagnostic);
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // 🚀 OPTYMALIZACJA: Filtruj po ulicy
            var filteredKody = FilterByUlica(kodyPocztowe, foundUlica.Id);
            diagnostic?.Log($"Po filtracji po ulicy (ID: {foundUlica.Id}): {filteredKody.Count} kodów");

            // Jeśli brak kodów dla ulicy, spróbuj zwrócić kod miejscowości
            if (filteredKody.Count == 0)
            {
                diagnostic?.Log("Ulica nie ma przypisanych kodów pocztowych");
                return TryReturnCityPostalCode(request, foundMiasto, foundUlica, combinedBuildingNumber, diagnostic);
            }

            // Filtruj po numerze domu (użyj połączonego numeru)
            if (!string.IsNullOrWhiteSpace(combinedBuildingNumber))
            {
                var beforeFilter = filteredKody.Count;
                filteredKody = FilterByBuildingNumber(filteredKody, combinedBuildingNumber);
                diagnostic?.Log($"Po filtracji po numerze domu '{combinedBuildingNumber}': {filteredKody.Count} kodów (było: {beforeFilter})");

                // 🆕 Jeśli nie znaleziono i numer ma literki (np. 30A), spróbuj bez literki
                if (filteredKody.Count == 0 && System.Text.RegularExpressions.Regex.IsMatch(combinedBuildingNumber, @"\d+[A-Za-z]"))
                {
                    // Wyciągnij samą liczbę (30A → 30)
                    var numberOnly = System.Text.RegularExpressions.Regex.Match(combinedBuildingNumber, @"^\d+").Value;

                    if (!string.IsNullOrEmpty(numberOnly))
                    {
                        diagnostic?.Log($"Retry bez literki: '{numberOnly}'");
                        filteredKody = FilterByBuildingNumber(kodyPocztowe.Where(k => k.UlicaId == foundUlica.Id).ToList(), numberOnly);
                        diagnostic?.Log($"Po filtracji po numerze '{numberOnly}': {filteredKody.Count} kodów");
                    }
                }
            }

            // ✅ Zwróć wynik bez filtrowania po kodzie źródłowym!
            return CreateResult(filteredKody, foundMiasto, foundUlica, combinedBuildingNumber, request.NumerMieszkania, diagnostic);
        }

        /// <summary>
        /// Wyszukiwanie bez ulicy - ZOPTYMALIZOWANA WERSJA
        /// </summary>
        private AddressSearchResult SearchWithoutStreet(
            AddressSearchRequest request,
            List<Miasto> miasta,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Szukanie bez ulicy ---");

            Miasto? selectedMiasto = null;

            // Jeśli podano kod pocztowy, użyj go do wyboru miejscowości
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy) && miasta.Count > 1)
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                diagnostic?.Log($"Próba zawężenia miejscowości po kodzie pocztowym: {kodNorm}");

                foreach (var miasto in miasta)
                {
                    if (_cache.TryGetKodyPocztowe(miasto.Id, out var kody))
                    {
                        // 🚀 OPTYMALIZACJA: for zamiast LINQ
                        for (int i = 0; i < kody.Count; i++)
                        {
                            if (kody[i].Kod == kodNorm)
                            {
                                selectedMiasto = miasto;
                                diagnostic?.Log($"✓ Wybrano miejscowość po kodzie: {miasto.Nazwa} (ID: {miasto.Id})");
                                break;
                            }
                        }
                    }
                    if (selectedMiasto != null) break;
                }
            }

            // Jeśli nie wybrano po kodzie, weź pierwszą
            if (selectedMiasto == null)
            {
                selectedMiasto = miasta[0];
                diagnostic?.Log($"Wybrano pierwszą miejscowość: {selectedMiasto.Nazwa} (ID: {selectedMiasto.Id})");
            }

            // Znajdź kody pocztowe dla miejscowości bez ulicy
            if (!_cache.TryGetKodyPocztowe(selectedMiasto.Id, out var kodyPocztowe))
            {
                diagnostic?.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {selectedMiasto.Id}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = selectedMiasto,
                    Message = $"Brak kodów pocztowych dla miejscowości {request.Miasto}",
                    NormalizedBuildingNumber = request.NumerDomu,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            diagnostic?.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // 🚀 OPTYMALIZACJA: Filtruj tylko kody bez ulicy
            var filteredKody = FilterWithoutStreet(kodyPocztowe);
            diagnostic?.Log($"Po filtracji bez ulicy: {filteredKody.Count} kodów");

            // Filtruj po numerze domu (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.NumerDomu))
            {
                var beforeFilter = filteredKody.Count;
                filteredKody = FilterByBuildingNumber(filteredKody, request.NumerDomu);
                diagnostic?.Log($"Po filtracji po numerze domu '{request.NumerDomu}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            // Filtruj po kodzie pocztowym (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                var beforeFilter = filteredKody.Count;
                filteredKody = FilterByPostalCode(filteredKody, kodNorm);
                diagnostic?.Log($"Po filtracji po kodzie '{kodNorm}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            return CreateResult(filteredKody, selectedMiasto, null, request.NumerDomu, request.NumerMieszkania, diagnostic);
        }

        /// <summary>
        /// Próbuje zwrócić kod pocztowy miasta, gdy ulica nie ma przypisanego kodu
        /// </summary>
        private AddressSearchResult TryReturnCityPostalCode(
            AddressSearchRequest request,
            Miasto miasto,
            Ulica ulica,
            string normalizedBuildingNumber,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- STRATEGIA: Zwracanie kodu miasta dla ulicy bez kodu ---");

            // Pobierz wszystkie kody dla miejscowości
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

            // 🚀 OPTYMALIZACJA: Znajdź kod miejscowości (bez ulicy) bez LINQ
            KodPocztowy? cityCode = null;
            for (int i = 0; i < kodyPocztowe.Count; i++)
            {
                if (kodyPocztowe[i].UlicaId == -1 || kodyPocztowe[i].UlicaId == null)
                {
                    cityCode = kodyPocztowe[i];
                    break;
                }
            }

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

        /// <summary>
        /// Tworzy wynik wyszukiwania na podstawie przefiltrowanych kodów pocztowych
        /// </summary>
        private AddressSearchResult CreateResult(
            List<KodPocztowy> kodyPocztowe,
            Miasto miasto,
            Ulica? ulica,
            string? normalizedBuildingNumber,
            string? normalizedApartmentNumber,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log("\n--- TWORZENIE WYNIKU ---");

            if (kodyPocztowe.Count == 0)
            {
                diagnostic?.Log("✗ Nie znaleziono żadnych pasujących kodów pocztowych");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miasto = miasto,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // Jeśli tylko jeden kod pocztowy, zwróć bezpośrednio
            if (kodyPocztowe.Count == 1)
            {
                var kod = kodyPocztowe[0];
                diagnostic?.Log($"Jedno dopasowanie: {kod.Kod}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = kod,
                    Miasto = miasto,
                    Ulica = ulica,
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // Więcej niż jeden kod - zwróć pierwszy z informacją o wielu dopasowaniach
            diagnostic?.Log($"⚠ Znaleziono wiele dopasowań: {kodyPocztowe.Count}");
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                Miasto = miasto,
                Ulica = ulica,
                KodPocztowy = kodyPocztowe[0],
                AlternativeMatches = kodyPocztowe,
                Message = $"Znaleziono wiele dopasowań ({kodyPocztowe.Count})",
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = normalizedApartmentNumber,
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

        /// <summary>
        /// Łączy numery: wyciągnięty z ulicy + podany w żądaniu
        /// </summary>
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

        /// <summary>
        /// Filtruje kody pocztowe po ID ulicy
        /// </summary>
        private List<KodPocztowy> FilterByUlica(List<KodPocztowy> kody, int ulicaId)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].UlicaId == ulicaId)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe bez ulicy
        /// </summary>
        private List<KodPocztowy> FilterWithoutStreet(List<KodPocztowy> kody)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].UlicaId == -1 || kody[i].UlicaId == null)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe po numerze budynku
        /// </summary>
        private List<KodPocztowy> FilterByBuildingNumber(List<KodPocztowy> kody, string numerBudynku)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (_numberValidator.IsNumberInRange(numerBudynku, kody[i].Numery))
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe po kodzie pocztowym
        /// </summary>
        private List<KodPocztowy> FilterByPostalCode(List<KodPocztowy> kody, string kodPocztowy)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].Kod == kodPocztowy)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        #endregion
    }
}