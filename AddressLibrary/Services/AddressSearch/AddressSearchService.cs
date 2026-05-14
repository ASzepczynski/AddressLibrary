using AddressLibrary.Cache;
using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Services.AddressSearch.Filters;
using AddressLibrary.Services.AddressSearch.Strategies;
using AddressLibrary.Dictionaries.CechyUlic;

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

        public AddressSearchService(AddressDbContext context, string appDataPath, AppCache? appCache = null)
        {
            _context     = context;
            _appDataPath = appDataPath;
            // Jeśli AppCache dostarczony z zewnątrz — AddressSearchCache korzysta z tych samych instancji
            _cache = new AddressSearchCache(context, _appDataPath);

            searchLogger = new SearchLogger(_appDataPath);
            _corrections = new NameCorrectionHelper(appDataPath);
        }

        public async Task InitializeAsync()
        {
            await _cache.InitializeAsync();

            var streetParser = new StreetParser(_context, _cache);
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
                    // wyczyścić ulicę
                    request.Ulica = "";
                }
            }

            if (_corrections.TryCorrect("M", request.Miasto, out string correctedCity))
            {
                Console.WriteLine($"Skorygowano miasto: '{request.Miasto}' -> '{correctedCity}'");
                request.Miasto = correctedCity;
            }

            if (_corrections.TryCorrect("U", request.Ulica, out var correctedStreet))
            {
                Console.WriteLine($"Skorygowano ulicę: '{request.Ulica}' -> '{correctedStreet}'");
                request.Ulica = correctedStreet;
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


            AddressSearchResult res;
            // Wybierz strategię wyszukiwania
            if (string.IsNullOrWhiteSpace(request.Ulica))
            {
                return _noStreetSearch!.Execute(request, miasta, searchLogger);
            }
            res = _streetSearch!.Execute(request, miasta, searchLogger);

            if (res.Status != AddressSearchStatus.InvalidStreetName && res.Status != AddressSearchStatus.UlicaNotFound)
            {
                return res;
            }
            // Jeśli nie znaleziono ulicy
            // Sprawdź czy ulica nie jest przypadkiem miejscowością
            var noweMiasto = request.Ulica;
            // Usunięcie prefiksu ul. czy os.
            (var cecha,noweMiasto) = CechyUlicUtils.SplitStreetPrefix(noweMiasto);
            var noweMiasta = CityUtils.FindAllMiasta(_cache, noweMiasto, request.KodPocztowy, searchLogger, out method);
            if (noweMiasta!=null && noweMiasta.Count >= 0)
            {
                var nowyRequest = CloneHelper.Klonuj(request);
                nowyRequest.Miasto = noweMiasto;
                nowyRequest.Ulica = "";

                var res2 = _noStreetSearch!.Execute(nowyRequest, noweMiasta, searchLogger);
                if (res2 != null) res = res2;
            }

            return res;
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