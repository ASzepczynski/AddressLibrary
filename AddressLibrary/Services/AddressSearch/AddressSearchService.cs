// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Services.AddressSearch.Strategies;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Główny serwis do wyszukiwania adresów (orchestrator)
    /// </summary>
    public class AddressSearchService
    {
        private readonly AddressSearchCache _cache;
        private readonly TextNormalizer _normalizer;
        private readonly StreetSearchStrategy _streetSearch;
        private readonly NoStreetSearchStrategy _noStreetSearch;

        public AddressSearchService(AddressDbContext context)
        {
            _normalizer = new TextNormalizer();
            _cache = new AddressSearchCache(context, _normalizer);

            var streetMatcher = new StreetMatcher(_normalizer);
            var numberValidator = new BuildingNumberValidator();
            var filters = new PostalCodeFilters(numberValidator);
            var resultFactory = new SearchResultFactory(_cache);
            var cityStrategy = new CityPostalCodeStrategy(_cache, filters);

            _streetSearch = new StreetSearchStrategy(_cache, _normalizer, streetMatcher, filters, cityStrategy, resultFactory);
            _noStreetSearch = new NoStreetSearchStrategy(_cache, _normalizer, filters, resultFactory);
        }

        public async Task InitializeAsync()
        {
            await _cache.InitializeAsync();
        }

        public async Task<AddressSearchResult> SearchAsync(
            AddressSearchRequest request,
            bool enableDiagnostics = false)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            DiagnosticLogger? diagnostic = enableDiagnostics ? new DiagnosticLogger() : null;

            // ✅ Walidacja: Miasto jest wymagane
            if (string.IsNullOrWhiteSpace(request.Miasto))
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Nazwa miejscowości jest wymagana"
                };
            }

            // ✅ NORMALIZACJA: Jeśli miasto i ulica są identyczne, wyczyść ulicę
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                var miastoNorm = _normalizer.Normalize(request.Miasto);
                var ulicaNorm = _normalizer.Normalize(request.Ulica);

                if (miastoNorm == ulicaNorm)
                {
                    diagnostic?.Log($"⚠ UWAGA: Miasto i ulica są identyczne ('{request.Miasto}' == '{request.Ulica}'). Wyczyśzczono ulicę.");

                    // Utwórz nowy request z wyczyszczoną ulicą
                    request = new AddressSearchRequest
                    {
                        KodPocztowy = request.KodPocztowy,
                        Miasto = request.Miasto,
                        Ulica = string.Empty, // ✅ Wyczyść ulicę
                        NumerDomu = request.NumerDomu,
                        NumerMieszkania = request.NumerMieszkania
                    };
                }
            }

            // Znajdź miasta o podanej nazwie
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

            // Wybierz strategię wyszukiwania
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                return _streetSearch.Execute(request, miasta, diagnostic);
            }
            else
            {
                return _noStreetSearch.Execute(request, miasta, diagnostic);
            }
        }

        public async Task<List<AddressSearchResult>> SearchBatchAsync(IEnumerable<AddressSearchRequest> requests)
        {
            if (!_cache.IsInitialized)
            {
                await InitializeAsync();
            }

            var results = new List<AddressSearchResult>();
            foreach (var request in requests)
            {
                var result = await SearchAsync(request, enableDiagnostics: false);
                results.Add(result);
            }
            return results;
        }

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
    }
}