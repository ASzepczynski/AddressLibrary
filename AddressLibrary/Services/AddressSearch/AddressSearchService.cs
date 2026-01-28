// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using AddressLibrary.Helpers;
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
            var ambiguityResolver = new AmbiguousStreetResolver(_normalizer); // 🆕 DODANE

            _streetSearch = new StreetSearchStrategy(_cache, _normalizer, streetMatcher, filters, cityStrategy, resultFactory, ambiguityResolver); // 🆕 DODANE parametr
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
            var miasta = FindAllMiasta(request.Miasto, request.KodPocztowy, diagnostic); // ✅ ZMIENIONE: dodano request.KodPocztowy
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

        private List<Miasto>? FindAllMiasta(
            string miastoName, 
            string? postalCode, // 🆕 DODANE
            DiagnosticLogger? diagnostic)
        {
            var miastoNorm = _normalizer.Normalize(miastoName);
            diagnostic?.Log($"Znormalizowana miejscowość: '{miastoName}' -> '{miastoNorm}'");

            if (_cache.TryGetMiasta(miastoNorm, out var miasta))
            {
                diagnostic?.Log($"Znaleziono {miasta.Count} miejscowości o nazwie '{miastoNorm}'");
                
                // ✅ Jeśli jest więcej niż 1 miasto, spróbuj wybrać najbardziej pasujące
                if (miasta.Count > 1)
                {
                    var bestCity = SelectBestCity(miasta, miastoName, postalCode, diagnostic); // 🆕 DODANE postalCode
                    if (bestCity != null)
                    {
                        diagnostic?.Log($"  ✓ Wybrano najlepiej pasującą miejscowość: '{bestCity.Nazwa}'");
                        return new List<Miasto> { bestCity };
                    }
                    
                    diagnostic?.Log($"  ⚠ Nie można jednoznacznie wybrać miejscowości - zwracam wszystkie {miasta.Count}");
                }
                
                return miasta;
            }

            // 🆕 FUZZY MATCHING z walidacją kodu pocztowego
            diagnostic?.Log($"  ✗ Nie znaleziono dokładnego dopasowania dla '{miastoNorm}'");
            diagnostic?.Log($"  🔍 Szukam podobnej miejscowości (fuzzy matching)...");

            var similarCity = FindSimilarCity(miastoNorm, postalCode, diagnostic); // 🆕 DODANE postalCode
            if (similarCity != null)
            {
                diagnostic?.Log($"  ✓ Znaleziono podobną miejscowość: '{similarCity.Nazwa}'");
                return new List<Miasto> { similarCity };
            }

            diagnostic?.Log($"  ✗ Nie znaleziono podobnej miejscowości");
            return null;
        }

        /// <summary>
        /// 🆕 Znajduje najbardziej podobną miejscowość używając odległości Levenshteina i tokenizacji
        /// ✅ WALIDUJE kod pocztowy jeśli został podany
        /// </summary>
        private Miasto? FindSimilarCity(
            string normalizedCityName, 
            string? postalCode, // 🆕 DODANE
            DiagnosticLogger? diagnostic)
        {
            var allCities = _cache.GetAllCities();
            
            if (allCities == null || allCities.Count == 0)
                return null;

            // ✅ Normalizuj kod pocztowy jeśli podano
            string? normalizedPostalCode = null;
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                normalizedPostalCode = UliceUtils.NormalizujKodPocztowy(postalCode);
                diagnostic?.Log($"    Wymagany kod pocztowy: '{normalizedPostalCode}'");
            }

            MiastoCached? bestMatch = null;
            int bestScore = int.MinValue;
            const int minScore = 5;

            var searchTokens = normalizedCityName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var cityCache in allCities)
            {
                // ✅ WALIDACJA KODU POCZTOWEGO - jeśli podano kod, miasto MUSI go mieć!
                if (normalizedPostalCode != null)
                {
                    if (!_cache.TryGetKodyPocztowe(cityCache.Miasto.Id, out var cityCodes))
                    {
                        continue; // Pomiń miasta bez kodów pocztowych
                    }

                    bool hasMatchingCode = cityCodes.Any(k => k.Kod == normalizedPostalCode);
                    if (!hasMatchingCode)
                    {
                        continue; // ✅ POMIŃ miasto jeśli kod się NIE ZGADZA!
                    }
                }

                int score = 0;

                // ✅ METODA 1: Dokładne dopasowanie
                if (cityCache.NormalizedNazwa == normalizedCityName)
                {
                    score = 100;
                }
                // ✅ METODA 2: Odległość Levenshteina
                else
                {
                    var distance = CalculateLevenshteinDistance(normalizedCityName, cityCache.NormalizedNazwa);
                    if (distance <= 2)
                    {
                        score = 50 - (distance * 10);
                    }
                }

                // ✅ METODA 3: Partial matching z tokenizacją
                if (searchTokens.Length > 0)
                {
                    var cityTokens = cityCache.NormalizedNazwa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int tokenScore = 0;

                    for (int i = 0; i < searchTokens.Length && i < cityTokens.Length; i++)
                    {
                        if (cityTokens[i] == searchTokens[i])
                        {
                            tokenScore += 15;
                        }
                        else if (cityTokens[i].StartsWith(searchTokens[i]))
                        {
                            tokenScore += 10;
                        }
                        else if (searchTokens[i].StartsWith(cityTokens[i]))
                        {
                            tokenScore += 8;
                        }
                        else
                        {
                            var tokenDist = CalculateLevenshteinDistance(searchTokens[i], cityTokens[i]);
                            if (tokenDist <= 2)
                            {
                                tokenScore += Math.Max(0, 7 - (tokenDist * 2));
                            }
                        }
                    }

                    if (searchTokens.Length > 0 && tokenScore >= searchTokens.Length * 5)
                    {
                        tokenScore += 10;
                    }

                    score = Math.Max(score, tokenScore);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = cityCache;
                }
            }

            if (bestMatch != null && bestScore >= minScore)
            {
                diagnostic?.Log($"    Najlepsze dopasowanie: '{bestMatch.Miasto.Nazwa}' (score: {bestScore})");
                return bestMatch.Miasto;
            }

            diagnostic?.Log($"    Brak dopasowania (najlepszy score: {bestScore}, wymagany: {minScore})");
            return null;
        }

        /// <summary>
        /// Oblicza odległość Levenshteina między dwoma ciągami znaków
        /// </summary>
        private int CalculateLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// 🆕 Wybiera najlepiej pasującą miejscowość z listy (gdy jest wiele o tej samej znormalizowanej nazwie)
        /// </summary>
        private Miasto? SelectBestCity(
            List<Miasto> miasta, 
            string originalCityName, 
            string? postalCode, 
            DiagnosticLogger? diagnostic)
        {
            if (miasta.Count == 1)
                return miasta[0];

            diagnostic?.Log($"  🔍 Wybór najlepszej z {miasta.Count} miejscowości...");

            // ✅ KRYTERIUM 0: Jeśli podano kod pocztowy, ODFILTRUJ miasta bez tego kodu
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                var normalizedCode = UliceUtils.NormalizujKodPocztowy(postalCode);
                diagnostic?.Log($"    Filtrowanie po kodzie pocztowym: '{normalizedCode}'");
                
                var citiesWithCode = miasta.Where(m =>
                {
                    if (_cache.TryGetKodyPocztowe(m.Id, out var codes))
                    {
                        bool hasCode = codes.Any(k => k.Kod == normalizedCode);
                        if (hasCode)
                        {
                            diagnostic?.Log($"      ✓ '{m.Nazwa}' (ID:{m.Id}) ma kod '{normalizedCode}'");
                        }
                        return hasCode;
                    }
                    diagnostic?.Log($"      ✗ '{m.Nazwa}' (ID:{m.Id}) nie ma kodów pocztowych");
                    return false;
                }).ToList();

                if (citiesWithCode.Count == 1)
                {
                    diagnostic?.Log($"    → Wybrano przez kod pocztowy: '{citiesWithCode[0].Nazwa}'");
                    return citiesWithCode[0];
                }

                if (citiesWithCode.Count > 0)
                {
                    miasta = citiesWithCode; // Ogranicz dalsze wyszukiwanie
                    diagnostic?.Log($"    → Zawężono do {miasta.Count} miast z kodem '{normalizedCode}'");
                }
                else
                {
                    diagnostic?.Log($"    ⚠ ŻADNE miasto nie ma kodu '{normalizedCode}' - kontynuuj bez filtracji");
                }
            }

            // ✅ KRYTERIUM 1: Dokładne dopasowanie oryginalnej nazwy (case-insensitive)
            var exactMatch = miasta.FirstOrDefault(m => 
                m.Nazwa.Equals(originalCityName, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
            {
                diagnostic?.Log($"    → Dokładne dopasowanie: '{exactMatch.Nazwa}'");
                return exactMatch;
            }

            // ✅ KRYTERIUM 2: Tokenizacja i partial matching (dla "OSTROWIEC ŚW." → "Ostrowiec Świętokrzyski")
            var normalizedOriginal = _normalizer.Normalize(originalCityName);
            var originalTokens = normalizedOriginal.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var cityScores = miasta.Select(m =>
            {
                var normalizedCity = _normalizer.Normalize(m.Nazwa);
                var cityTokens = normalizedCity.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                int score = 0;

                // Sprawdź dokładne dopasowanie całości
                if (normalizedCity == normalizedOriginal)
                {
                    score = 100;
                }
                else
                {
                    // Odległość Levenshteina
                    var distance = CalculateLevenshteinDistance(normalizedOriginal, normalizedCity);
                    score = Math.Max(0, 50 - (distance * 5));

                    // Tokenizacja - dopasowanie fragmentów
                    int tokenScore = 0;
                    for (int i = 0; i < originalTokens.Length && i < cityTokens.Length; i++)
                    {
                        if (cityTokens[i] == originalTokens[i])
                        {
                            tokenScore += 20; // Dokładne dopasowanie tokenu
                        }
                        else if (cityTokens[i].StartsWith(originalTokens[i]))
                        {
                            tokenScore += 15; // Prefix match
                        }
                        else if (originalTokens[i].StartsWith(cityTokens[i]))
                        {
                            tokenScore += 10; // Partial match
                        }
                        else
                        {
                            var tokenDist = CalculateLevenshteinDistance(originalTokens[i], cityTokens[i]);
                            tokenScore += Math.Max(0, 10 - (tokenDist * 3));
                        }
                    }

                    score = Math.Max(score, tokenScore);
                }

                return new { City = m, Score = score };
            }).OrderByDescending(x => x.Score).ToList();

            var best = cityScores.FirstOrDefault();
            if (best != null && best.Score > 0)
            {
                diagnostic?.Log($"    → Najlepszy match: '{best.City.Nazwa}' (score: {best.Score})");
                
                // Debug: pokaż wszystkie wyniki
                foreach (var result in cityScores.Take(3))
                {
                    diagnostic?.Log($"       {result.City.Nazwa}: score={result.Score}");
                }
                
                return best.City;
            }

            diagnostic?.Log($"    → Brak jednoznacznego wyboru");
            return null;
        }
    }
}