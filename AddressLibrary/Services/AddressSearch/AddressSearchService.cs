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
        /// Wyszukuje adres na podstawie parametrów
        /// </summary>
        public async Task<AddressSearchResult> SearchAsync(AddressSearchRequest request)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            var diagnostic = new DiagnosticLogger();

            // Walidacja
            if (string.IsNullOrWhiteSpace(request.Miejscowosc))
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Nazwa miejscowości jest wymagana"
                };
            }

            // Krok 1: Znajdź miejscowość (lub miejscowości o tej nazwie)
            var miejscowosci = FindAllMiejscowosci(request.Miejscowosc, diagnostic);
            if (miejscowosci == null || miejscowosci.Count == 0)
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.MiejscowoscNotFound,
                    Message = $"Nie znaleziono miejscowości: {request.Miejscowosc}",
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            // Krok 2: Znajdź ulicę (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                return SearchWithStreet(request, miejscowosci, diagnostic);
            }
            else
            {
                return SearchWithoutStreet(request, miejscowosci, diagnostic);
            }
        }

        /// <summary>
        /// Masowe wyszukiwanie adresów
        /// </summary>
        public async Task<List<AddressSearchResult>> SearchBatchAsync(IEnumerable<AddressSearchRequest> requests)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            var results = new List<AddressSearchResult>();

            foreach (var request in requests)
            {
                var result = await SearchAsync(request);
                results.Add(result);
            }

            return results;
        }

        #region Private Methods

        /// <summary>
        /// Znajduje wszystkie miejscowości o podanej nazwie
        /// </summary>
        private List<Miejscowosc>? FindAllMiejscowosci(string miejscowoscName, DiagnosticLogger diagnostic)
        {
            var miejscowoscNorm = _normalizer.Normalize(miejscowoscName);
            diagnostic.Log($"Znormalizowana miejscowość: '{miejscowoscName}' -> '{miejscowoscNorm}'");

            if (!_cache.TryGetMiejscowosci(miejscowoscNorm, out var miejscowosci))
            {
                return null;
            }

            diagnostic.Log($"Znaleziono {miejscowosci.Count} miejscowości o nazwie '{miejscowoscNorm}'");
            return miejscowosci;
        }

        /// <summary>
        /// Wyszukiwanie z podaną ulicą - NOWA STRATEGIA
        /// </summary>
        private AddressSearchResult SearchWithStreet(
            AddressSearchRequest request,
            List<Miejscowosc> miejscowosci,
            DiagnosticLogger diagnostic)
        {
            diagnostic.Log("\n--- STRATEGIA: Szukanie z ulicą ---");

            // Normalizuj ulicę i wyciągnij numer (jeśli jest)
            var (normalizedStreet, extractedNumber) = _normalizer.NormalizeStreetWithNumber(request.Ulica);

            if (!string.IsNullOrEmpty(extractedNumber))
            {
                diagnostic.Log($"Wyciągnięto numer z ulicy: '{extractedNumber}'");
            }

            // Skonkatenuj numery: wyciągnięty z ulicy + podany w żądaniu
            var combinedBuildingNumber = CombineNumbers(extractedNumber, request.NumerDomu);
            diagnostic.Log($"Połączony numer budynku: '{combinedBuildingNumber}'");

            // Szukaj ulicy we wszystkich miejscowościach o tej nazwie
            Ulica? foundUlica = null;
            Miejscowosc? foundMiejscowosc = null;

            foreach (var miejscowosc in miejscowosci)
            {
                if (_cache.TryGetUlice(miejscowosc.Id, out var ulice))
                {
                    diagnostic.Log($"Sprawdzam miejscowość: {miejscowosc.Nazwa} (ID: {miejscowosc.Id}), ulic: {ulice.Count}");

                    var ulica = ulice.FirstOrDefault(u => _streetMatcher.IsMatch(u.Nazwa1, u.Nazwa2, normalizedStreet));

                    if (ulica != null)
                    {
                        foundUlica = ulica;
                        foundMiejscowosc = miejscowosc;
                        diagnostic.Log($"✓ Znaleziono ulicę: {ulica.Cecha} {ulica.Nazwa1} w {miejscowosc.Nazwa} (ID: {ulica.Id})");
                        break;
                    }
                }
            }

            if (foundUlica == null || foundMiejscowosc == null)
            {
                diagnostic.Log($"✗ Nie znaleziono ulicy '{request.Ulica}' w żadnej z miejscowości");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.UlicaNotFound,
                    Miejscowosc = miejscowosci[0],
                    Message = $"Nie znaleziono ulicy '{request.Ulica}' w miejscowości {request.Miejscowosc}",
                    NormalizedBuildingNumber = combinedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            // Sprawdź kody pocztowe dla znalezionej ulicy
            if (!_cache.TryGetKodyPocztowe(foundMiejscowosc.Id, out var kodyPocztowe))
            {
                diagnostic.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {foundMiejscowosc.Id}");
                return TryReturnCityPostalCode(request, foundMiejscowosc, foundUlica, combinedBuildingNumber, diagnostic);
            }

            diagnostic.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // Filtruj po ulicy
            var filteredKody = kodyPocztowe.Where(k => k.UlicaId == foundUlica.Id).ToList();
            diagnostic.Log($"Po filtracji po ulicy (ID: {foundUlica.Id}): {filteredKody.Count} kodów");

            // Jeśli brak kodów dla ulicy, spróbuj zwrócić kod miejscowości
            if (filteredKody.Count == 0)
            {
                diagnostic.Log("Ulica nie ma przypisanych kodów pocztowych");
                return TryReturnCityPostalCode(request, foundMiejscowosc, foundUlica, combinedBuildingNumber, diagnostic);
            }

            // Filtruj po numerze domu (użyj połączonego numeru)
            if (!string.IsNullOrWhiteSpace(combinedBuildingNumber))
            {
                var beforeFilter = filteredKody.Count;
                filteredKody = filteredKody
                    .Where(k => _numberValidator.IsNumberInRange(combinedBuildingNumber, k.Numery))
                    .ToList();
                diagnostic.Log($"Po filtracji po numerze domu '{combinedBuildingNumber}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            // Jeśli mamy dokładnie jeden wynik, zwróć go
            if (filteredKody.Count == 1)
            {
                diagnostic.Log($"✓ Sukces! Znaleziono dokładnie jeden kod: {filteredKody[0].Kod}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = filteredKody[0],
                    Miejscowosc = foundMiejscowosc,
                    Ulica = foundUlica,
                    NormalizedBuildingNumber = combinedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            // Jeśli wiele wyników, użyj kodu pocztowego do zawężenia
            if (filteredKody.Count > 1 && !string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                var beforeFilter = filteredKody.Count;
                var matchedByCode = filteredKody.Where(k => k.Kod == kodNorm).ToList();
                diagnostic.Log($"Po filtracji po kodzie '{kodNorm}': {matchedByCode.Count} kodów (było: {beforeFilter})");

                if (matchedByCode.Count > 0)
                {
                    filteredKody = matchedByCode;
                }
            }

            return CreateResult(filteredKody, foundMiejscowosc, foundUlica, combinedBuildingNumber, request.NumerMieszkania, diagnostic);
        }

        /// <summary>
        /// Wyszukiwanie bez ulicy - używa kodu pocztowego do dopasowania miejscowości
        /// </summary>
        private AddressSearchResult SearchWithoutStreet(
            AddressSearchRequest request,
            List<Miejscowosc> miejscowosci,
            DiagnosticLogger diagnostic)
        {
            diagnostic.Log("\n--- STRATEGIA: Szukanie bez ulicy ---");

            Miejscowosc? selectedMiejscowosc = null;

            // Jeśli podano kod pocztowy, użyj go do wyboru miejscowości
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy) && miejscowosci.Count > 1)
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                diagnostic.Log($"Próba zawężenia miejscowości po kodzie pocztowym: {kodNorm}");

                foreach (var miejscowosc in miejscowosci)
                {
                    if (_cache.TryGetKodyPocztowe(miejscowosc.Id, out var kody) &&
                        kody.Any(k => k.Kod == kodNorm))
                    {
                        selectedMiejscowosc = miejscowosc;
                        diagnostic.Log($"✓ Wybrano miejscowość po kodzie: {miejscowosc.Nazwa} (ID: {miejscowosc.Id})");
                        break;
                    }
                }
            }

            // Jeśli nie wybrano po kodzie, weź pierwszą
            if (selectedMiejscowosc == null)
            {
                selectedMiejscowosc = miejscowosci[0];
                diagnostic.Log($"Wybrano pierwszą miejscowość: {selectedMiejscowosc.Nazwa} (ID: {selectedMiejscowosc.Id})");
            }

            // Znajdź kody pocztowe dla miejscowości bez ulicy
            if (!_cache.TryGetKodyPocztowe(selectedMiejscowosc.Id, out var kodyPocztowe))
            {
                diagnostic.Log($"✗ Brak kodów pocztowych dla miejscowości ID: {selectedMiejscowosc.Id}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miejscowosc = selectedMiejscowosc,
                    Message = $"Brak kodów pocztowych dla miejscowości {request.Miejscowosc}",
                    NormalizedBuildingNumber = request.NumerDomu,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            diagnostic.Log($"Znaleziono {kodyPocztowe.Count} kodów pocztowych dla miejscowości");

            // Filtruj tylko kody bez ulicy
            var filteredKody = kodyPocztowe.Where(k => k.UlicaId == -1 || k.UlicaId == null).ToList();
            diagnostic.Log($"Po filtracji bez ulicy: {filteredKody.Count} kodów");

            // Filtruj po numerze domu (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.NumerDomu))
            {
                var beforeFilter = filteredKody.Count;
                filteredKody = filteredKody
                    .Where(k => _numberValidator.IsNumberInRange(request.NumerDomu, k.Numery))
                    .ToList();
                diagnostic.Log($"Po filtracji po numerze domu '{request.NumerDomu}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            // Filtruj po kodzie pocztowym (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.KodPocztowy))
            {
                var kodNorm = _normalizer.NormalizePostalCode(request.KodPocztowy);
                var beforeFilter = filteredKody.Count;
                filteredKody = filteredKody.Where(k => k.Kod == kodNorm).ToList();
                diagnostic.Log($"Po filtracji po kodzie '{kodNorm}': {filteredKody.Count} kodów (było: {beforeFilter})");
            }

            return CreateResult(filteredKody, selectedMiejscowosc, null, request.NumerDomu, request.NumerMieszkania, diagnostic);
        }

        /// <summary>
        /// Próbuje zwrócić kod pocztowy miasta, gdy ulica nie ma przypisanego kodu
        /// </summary>
        private AddressSearchResult TryReturnCityPostalCode(
            AddressSearchRequest request,
            Miejscowosc miejscowosc,
            Ulica ulica,
            string normalizedBuildingNumber,
            DiagnosticLogger diagnostic)
        {
            diagnostic.Log("\n--- STRATEGIA: Zwracanie kodu miasta dla ulicy bez kodu ---");

            // Pobierz wszystkie kody dla miejscowości
            if (!_cache.TryGetKodyPocztowe(miejscowosc.Id, out var kodyPocztowe))
            {
                diagnostic.Log("✗ Brak kodów pocztowych dla miejscowości");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            // Znajdź kod miejscowości (bez ulicy)
            var cityCode = kodyPocztowe.FirstOrDefault(k => k.UlicaId == -1 || k.UlicaId == null);

            if (cityCode != null)
            {
                diagnostic.Log($"✓ Zwracam kod miejscowości: {cityCode.Kod} (ulica nie ma przypisanego kodu)");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = cityCode,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica,
                    Message = $"Zwrócono kod miejscowości (ulica nie ma przypisanego kodu)",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            // Jeśli nie ma kodu miasta, weź pierwszy dostępny
            var firstCode = kodyPocztowe.FirstOrDefault();
            if (firstCode != null)
            {
                diagnostic.Log($"✓ Zwracam pierwszy dostępny kod: {firstCode.Kod}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = firstCode,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica,
                    Message = $"Zwrócono pierwszy dostępny kod dla miejscowości",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = request.NumerMieszkania,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            diagnostic.Log("✗ Nie znaleziono żadnego kodu pocztowego");
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.KodPocztowyNotFound,
                Miejscowosc = miejscowosc,
                Ulica = ulica,
                Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = request.NumerMieszkania,
                DiagnosticInfo = diagnostic.GetLog()
            };
        }

        private AddressSearchResult CreateResult(
            List<KodPocztowy> filteredKody,
            Miejscowosc miejscowosc,
            Ulica? ulica,
            string? normalizedBuildingNumber,
            string? normalizedApartmentNumber,
            DiagnosticLogger diagnostic)
        {
            if (filteredKody.Count == 0)
            {
                diagnostic.Log("✗ Nie znaleziono żadnych pasujących kodów pocztowych");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica,
                    Message = "Nie znaleziono kodu pocztowego dla podanych parametrów",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            if (filteredKody.Count > 1)
            {
                diagnostic.Log($"⚠ Znaleziono wiele dopasowań: {filteredKody.Count}");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.MultipleMatches,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica,
                    KodPocztowy = filteredKody[0],
                    AlternativeMatches = filteredKody,
                    Message = $"Znaleziono wiele dopasowań ({filteredKody.Count})",
                    NormalizedBuildingNumber = normalizedBuildingNumber,
                    NormalizedApartmentNumber = normalizedApartmentNumber,
                    DiagnosticInfo = diagnostic.GetLog()
                };
            }

            diagnostic.Log($"✓ Sukces! Znaleziono kod: {filteredKody[0].Kod}");
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.Success,
                KodPocztowy = filteredKody[0],
                Miejscowosc = miejscowosc,
                Ulica = ulica,
                NormalizedBuildingNumber = normalizedBuildingNumber,
                NormalizedApartmentNumber = normalizedApartmentNumber,
                DiagnosticInfo = diagnostic.GetLog()
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

            return $"{extractedNumber.Trim()} {providedNumber.Trim()}";
        }

        #endregion
    }
}