using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch;

namespace AddressLibrary.Helpers
{
    public static class CityUtils
    {
        public static List<Miasto>? FindAllMiasta(
            AddressSearchCache _cache,
            TextNormalizer _normalizer,
            string miastoName,
            string? postalCode, // 🆕 DODANE
            SearchLogger? searchLogger,
            out string? method)
        {
            var miastoNorm = _normalizer.Normalize(miastoName);
            searchLogger?.Log($"Znormalizowana miejscowość: '{miastoName}' -> '{miastoNorm}'");

            if (_cache.TryGetMiasta(miastoNorm, out var miasta))
            {
                searchLogger?.Log($"Znaleziono {miasta.Count} miejscowości o nazwie '{miastoNorm}'");

                // ✅ Jeśli jest więcej niż 1 miasto, spróbuj wybrać najbardziej pasujące
                if (miasta.Count > 1)
                {
                    var bestCity = SelectBestCity(_cache, miasta, miastoName, postalCode, searchLogger); // 🆕 DODANE postalCode
                    if (bestCity != null)
                    {
                        searchLogger?.Log($"  ✓ Wybrano najlepiej pasującą miejscowość: '{bestCity.Nazwa}'");
                        method = "CityBestFit";
                        return new List<Miasto> { bestCity };
                    }

                    searchLogger?.Log($"  ⚠ Nie można jednoznacznie wybrać miejscowości - zwracam wszystkie {miasta.Count}");
                }
                method = "AmbiguosCity";
                return miasta;
            }

            // 🆕 FUZZY MATCHING z walidacją kodu pocztowego
            searchLogger?.Log($"  ✗ Nie znaleziono dokładnego dopasowania dla '{miastoNorm}'");
            searchLogger?.Log($"  🔍 Szukam podobnej miejscowości (fuzzy matching)...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var similarCity = FindSimilarCity(_cache, miastoNorm, postalCode, searchLogger); // 🆕 DODANE postalCode
            stopwatch.Stop();
            searchLogger?.Log($"  ⏱ Czas wykonania FindSimilarCity: {stopwatch.ElapsedMilliseconds} ms");

            if (similarCity != null)
            {
                searchLogger?.Log($"  ✓ Znaleziono podobną miejscowość: '{similarCity.Nazwa}'");
                method = "FuzzyCity";
                return new List<Miasto> { similarCity };
            }

            searchLogger?.Log($"  ✗ Nie znaleziono podobnej miejscowości");
            method = null;
            return null;
        }

        /// <summary>
        /// 🆕 Znajduje najbardziej podobną miejscowość używając odległości Levenshteina i tokenizacji
        /// ✅ WALIDUJE kod pocztowy jeśli został podany
        /// </summary>
        public static Miasto? FindSimilarCity(
            AddressSearchCache _cache,
            string normalizedCityName,
            string? postalCode, // 🆕 DODANE
            GeneralLogger? searchLogger)
        {
            var allCities = _cache.GetAllCities();

            if (allCities == null || allCities.Count == 0)
                return null;

            searchLogger?.Log($"   KodPocztowy: '{postalCode}'");

            // ✅ Normalizuj kod pocztowy jeśli podano
            string? normalizedPostalCode = null;
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                normalizedPostalCode = UliceUtils.NormalizujKodPocztowy(postalCode);

                // ✅ POPRAWKA: Jeśli normalizacja zwróciła pusty string, zignoruj kod pocztowy
                if (string.IsNullOrEmpty(normalizedPostalCode))
                {
                    searchLogger?.Log($"    ⚠ Nieprawidłowy format kodu pocztowego: '{postalCode}' - ignoruję");
                    normalizedPostalCode = null;
                }
                else
                {
                    searchLogger?.Log($"    Wymagany kod pocztowy: '{normalizedPostalCode}'");
                }
            }

            // 🚀 OPTYMALIZACJA: Przefiltruj miasta PRZED główną pętlą
            List<MiastoCached> candidateCities = allCities;

            int DlugoscKodu = 3;

            if (normalizedPostalCode != null && normalizedPostalCode.Length >= 5)
            {
                string requiredPrefix = normalizedPostalCode.Substring(0, DlugoscKodu);
                searchLogger?.Log($"    🔍 Filtrowanie miast po prefiksie kodu: '{requiredPrefix}'");

                var filteredCities = new List<MiastoCached>();

                foreach (var cityCache in allCities)
                {
                    if (_cache.TryGetKodyPocztoweMiasta(cityCache.Miasto.Id, out var cityCodes))
                    {
                        bool hasMatchingCode = cityCodes.Any(k =>
                            !string.IsNullOrEmpty(k.Kod) &&
                            k.Kod.Length >= 5 &&
                            k.Kod.Substring(0, DlugoscKodu) == requiredPrefix);

                        if (hasMatchingCode)
                        {
                            filteredCities.Add(cityCache);
                        }
                    }
                }

                searchLogger?.Log($"    ✓ Zawężono z {allCities.Count} do {filteredCities.Count} miast (prefix: '{requiredPrefix}')");
                candidateCities = filteredCities;

                if (candidateCities.Count == 0)
                {
                    searchLogger?.Log($"    ✗ Brak miast z kodem zaczynającym się na '{requiredPrefix}'");
                    return null;
                }
            }

            MiastoCached? bestMatch = null;
            int bestScore = int.MinValue;
            const int minScore = 20;

            var searchTokens = normalizedCityName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int LiczbaMiast = 0;

            // 🚀 Iteruj tylko po przefiltrowanych miastach
            foreach (var cityCache in candidateCities)
            {
                int score = 0;
                LiczbaMiast++;

                // ✅ METODA 1: Dokładne dopasowanie
                if (cityCache.NormalizedNazwa == normalizedCityName)
                {
                    score = 100;
                }
                // ✅ METODA 2: Odległość Levenshteina
                else
                {
                    var distance = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(normalizedCityName, cityCache.NormalizedNazwa);
                    if (distance <= 2)
                    {
                        score = 50 - (distance * 10);
                    }
                }

                //// ✅ METODA 3: Partial matching z tokenizacją
                //if (searchTokens.Length > 0)
                //{
                //    var cityTokens = cityCache.NormalizedNazwa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                //    int tokenScore = 0;

                //    for (int i = 0; i < searchTokens.Length && i < cityTokens.Length; i++)
                //    {
                //        if (cityTokens[i] == searchTokens[i])
                //        {
                //            tokenScore += 15;
                //        }
                //        else if (cityTokens[i].StartsWith(searchTokens[i]))
                //        {
                //            tokenScore += 10;
                //        }
                //        else if (searchTokens[i].StartsWith(cityTokens[i]))
                //        {
                //            tokenScore += 8;
                //        }
                //        else
                //        {
                //            var tokenDist = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(searchTokens[i], cityTokens[i]);
                //            if (tokenDist <= 2)
                //            {
                //                tokenScore += Math.Max(0, 7 - (tokenDist * 2));
                //            }
                //        }
                //    }

                //    if (searchTokens.Length > 0 && tokenScore >= searchTokens.Length * 5)
                //    {
                //        tokenScore += 10;
                //    }

                //    score = Math.Max(score, tokenScore);
                //}

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = cityCache;
                }
            }

            searchLogger?.Log($" Przeanalizowano:{LiczbaMiast}");

            if (bestMatch != null && bestScore >= minScore)
            {
                searchLogger?.Log($"    Najlepsze dopasowanie zamiast {normalizedCityName}: '{bestMatch.Miasto.Nazwa}' (score: {bestScore})");
                return bestMatch.Miasto;
            }

            searchLogger?.Log($"    Brak dopasowania (najlepszy score: {bestScore}, wymagany: {minScore})");
            return null;
        }


        /// <summary>
        /// 🆕 Wybiera najlepiej pasującą miejscowość z listy (gdy jest wiele o tej samej znormalizowanej nazwie)
        /// </summary>
        private static Miasto? SelectBestCity(
            AddressSearchCache _cache,
            List<Miasto> miasta,
            string originalCityName,
            string? postalCode,
            SearchLogger? searchLogger)
        {
            if (miasta.Count == 1)
                return miasta[0];
            if (miasta.Count == 0)
                return null;

            searchLogger?.Log($"  🔍 Wybór najlepszej z {miasta.Count} miejscowości...");

            // ✅ KRYTERIUM 0: Jeśli podano kod pocztowy, ODFILTRUJ miasta bez tego kodu
            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                var normalizedCode = UliceUtils.NormalizujKodPocztowy(postalCode);
                searchLogger?.Log($"    Filtrowanie po kodzie pocztowym: '{normalizedCode}'");

                var citiesWithCode = miasta.Where(m =>
                {
                    if (_cache.TryGetKodyPocztoweMiasta(m.Id, out var codes))
                    {
                        bool hasCode = codes.Any(k => k.Kod == normalizedCode);
                        if (hasCode)
                        {
                            searchLogger?.Log($"      ✓ '{m.Nazwa}' (ID:{m.Id}) ma kod '{normalizedCode}'");
                        }
                        return hasCode;
                    }
                    searchLogger?.Log($"      ✗ '{m.Nazwa}' (ID:{m.Id}) nie ma kodów pocztowych");
                    return false;
                }).ToList();

                if (citiesWithCode.Count == 1)
                {
                    searchLogger?.Log($"    → Wybrano przez kod pocztowy: '{citiesWithCode[0].Nazwa}'");
                    return citiesWithCode[0];
                }

                if (citiesWithCode.Count > 0)
                {
                    miasta = citiesWithCode; // Ogranicz dalsze wyszukiwanie
                    searchLogger?.Log($"    → Zawężono do {miasta.Count} miast z kodem '{normalizedCode}'");
                }
                else
                {
                    searchLogger?.Log($"    ⚠ ŻADNE miasto nie ma kodu '{normalizedCode}' - kontynuuj bez filtracji");
                }
            }

            // ✅ KRYTERIUM 1: Dokładne dopasowanie oryginalnej nazwy (case-insensitive)
            var exactMatch = miasta.Where(m =>
                m.Nazwa.Equals(originalCityName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (exactMatch.Count == 1)
            {
                searchLogger?.Log($"    → Dokładne dopasowanie: '{exactMatch[0].Nazwa}'");
                return exactMatch[0];
            }
            searchLogger?.Log($"    → Brak jednoznacznego wyboru");
            return null;
        }

        public static (Miasto? city, bool wasFuzzy) FindSimilarCityWithMethod(
            AddressSearchCache _cache,
            string normalizedCityName,
            string? postalCode,
            GeneralLogger? searchLogger)
        {
            var city = FindSimilarCity(_cache, normalizedCityName, postalCode, searchLogger);

            // Jeśli znaleziono miasto i jego nazwa różni się od wyszukiwanej - to fuzzy
            bool wasFuzzy = city != null &&
                !city.Nazwa.Equals(normalizedCityName, StringComparison.OrdinalIgnoreCase);

            return (city, wasFuzzy);
        }
    }
}
