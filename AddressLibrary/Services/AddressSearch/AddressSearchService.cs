// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Services.AddressSearch.Strategies;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Główny serwis do wyszukiwania adresów (orchestrator)
    /// </summary>
    public class AddressSearchService : IDisposable
    {
        private readonly AddressDbContext _context; // ✅ DODANO: przechowuj context
        private readonly AddressSearchCache _cache;
        private StreetSearchStrategy? _streetSearch;
        private NoStreetSearchStrategy? _noStreetSearch;
        private string _appDataPath;
        private SearchLogger searchLogger;
        private bool _disposed = false;
        private readonly NameCorrectionHelper _corrections;

        public AddressSearchService(AddressDbContext context, string appDataPath)
        {
            _context = context; // ✅ DODANO: zapisz context
            _appDataPath = appDataPath;
            _cache = new AddressSearchCache(context, _appDataPath);

            searchLogger = new SearchLogger(_appDataPath);
            _corrections = new NameCorrectionHelper(appDataPath);
            Console.WriteLine($"Załadowano {_corrections.Count} korekt ({_corrections.GetCountByType("M")} miast, {_corrections.GetCountByType("U")} ulic)");
        }

        public async Task InitializeAsync()
        {
            await _cache.InitializeAsync();

            // ✅ POPRAWKA: Utwórz i zainicjalizuj StreetParser
            var streetParser = new StreetParser(_context);
            await streetParser.InitializeAsync();

            // ✅ POPRAWKA: Przekaż streetParser do StreetMatcher
            var streetMatcher = new StreetMatcher(streetParser);
            var filters = new PostalCodeFilters();
            var resultFactory = new SearchResultFactory(_cache);
            var cityStrategy = new CityPostalCodeStrategy(_cache, filters);
            var ambiguityResolver = new AmbiguousStreetResolver();

            _streetSearch = new StreetSearchStrategy(_cache, streetMatcher, filters, cityStrategy, resultFactory, ambiguityResolver);
            _noStreetSearch = new NoStreetSearchStrategy(_cache, filters, resultFactory);
        }

        /// <summary>
        /// Unieważnia cache — wymusza ponowne załadowanie danych przy następnym SearchAsync.
        /// Należy wywołać po załadowaniu nowych kodów pocztowych lub zmianie hierarchii.
        /// </summary>
        public async Task ReinitializeAsync()
        {
            _cache.Invalidate();
            await InitializeAsync();
        }

        public async Task<AddressSearchResult> SearchAsync(
            AddressSearchRequest request
            )
        {
            if (!_cache.IsInitialized || _streetSearch == null || _noStreetSearch == null)
            {
                await InitializeAsync();
            }

            searchLogger?.Log($"{Environment.NewLine}==== Rozpoczynam poszukiwanie ====");
            searchLogger?.Log($"  Kod: ({request.KodPocztowy})");
            searchLogger?.Log($"  Miasto: ({request.Miasto})");
            searchLogger?.Log($"  Ulica: ({request.Ulica})");
            searchLogger?.Log($"  Nr domu: ({request.NumerDomu})");
            searchLogger?.Log($"  Lokal: ({request.NumerMieszkania})");

            // ✅ Walidacja: Miasto jest wymagane
            if (string.IsNullOrWhiteSpace(request.Miasto))
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Nazwa miejscowości jest wymagana"
                };
            }
            (string sMiasto, string sNumer1) = UliceUtils.ExtractHouseNumberFromStreet(request.Miasto);
            (string sUlica, string sNumer2) = UliceUtils.ExtractHouseNumberFromStreet(request.Ulica);



            var NumerDomu = "";
            var NumerMieszkania = "";

            var elementy = new List<string>();

            if (!string.IsNullOrEmpty(sNumer1)) { elementy.Add(sNumer1); }
            if (!string.IsNullOrEmpty(sNumer2)) { elementy.Add(sNumer2); }
            if (!string.IsNullOrEmpty(request.NumerDomu)) { elementy.Add(request.NumerDomu); }
            if (!string.IsNullOrEmpty(request.NumerMieszkania)) { elementy.Add(request.NumerMieszkania); }

            if (elementy.Count() >= 1)
            {
                NumerDomu = elementy[0];
            }

            if (elementy.Count() >= 2)
            {
                NumerMieszkania = string.Join("/", elementy.Skip(1));
            }

            if (string.IsNullOrWhiteSpace(NumerDomu))
            {
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.ValidationError,
                    Message = "Numer domu jest wymagany"
                };
            }

            request = new AddressSearchRequest
            {
                KodPocztowy = request.KodPocztowy,
                Miasto = sMiasto,
                Ulica = sUlica,
                NumerDomu = NumerDomu,
                NumerMieszkania = NumerMieszkania
            };

            // ✅ NORMALIZACJA: Jeśli miasto i ulica są identyczne, wyczyść ulicę
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                var miastoNorm = TextNormalizer.Normalize(request.Miasto);
                var ulicaNorm = TextNormalizer.Normalize(request.Ulica);

                if (miastoNorm == ulicaNorm)
                {
                    searchLogger?.Log($"⚠ UWAGA: Miasto i ulica są identyczne ('{request.Miasto}' == '{request.Ulica}'). Wyczyśzczono ulicę.");

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

            if (_corrections.TryCorrect("M", request.Miasto, out string correctedCity))
            {
                Console.WriteLine($"Skorygowano miasto: '{request.Miasto}' -> '{correctedCity}'");
                request = new AddressSearchRequest
                {
                    KodPocztowy = request.KodPocztowy,
                    Miasto = correctedCity,
                    Ulica = request.Ulica,
                    NumerDomu = request.NumerDomu,
                    NumerMieszkania = request.NumerMieszkania
                };
            }

            if (_corrections.TryCorrect("U", request.Ulica, out var correctedStreet))
            {
                Console.WriteLine($"Skorygowano ulicę: '{request.Ulica}' -> '{correctedStreet}'");
                request = new AddressSearchRequest
                {
                    KodPocztowy = request.KodPocztowy,
                    Miasto = request.Miasto,
                    Ulica = correctedStreet,
                    NumerDomu = request.NumerDomu,
                    NumerMieszkania = request.NumerMieszkania
                };
            }

            // Znajdź miasta o podanej nazwie
            var miasta = CityUtils.FindAllMiasta(_cache, request.Miasto, request.KodPocztowy, searchLogger, out string? method);
            if (miasta == null || miasta.Count == 0)
            {
                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.MiastoNotFound,
                    Message = $"Nie znaleziono miejscowości: {request.Miasto}",
                };
                result.AddDiagnostic($"Szukana miejscowość: {request.Miasto}");
                result.AddDiagnostic($"Znormalizowana nazwa: {TextNormalizer.Normalize(request.Miasto)}");
                return result;
            }

            // Wybierz strategię wyszukiwania
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                return _streetSearch!.Execute(request, miasta, searchLogger);
            }
            else
            {
                return _noStreetSearch!.Execute(request, miasta, searchLogger);
            }
        }

        public async Task<List<AddressSearchResult>> SearchBatchAsync(IEnumerable<AddressSearchRequest> requests)
        {
            if (!_cache.IsInitialized || _streetSearch == null || _noStreetSearch == null)
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


        // ✅ IMPLEMENTACJA IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    searchLogger?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}