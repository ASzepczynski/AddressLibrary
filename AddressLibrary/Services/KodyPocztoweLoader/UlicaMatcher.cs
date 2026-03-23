// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.HierarchyBuilders;
using AddressLibrary.Services.AddressSearch;

namespace AddressLibrary.Services.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje ulice w miejscowościach z obsługą korekt
    /// </summary>
    internal class UlicaMatcher
    {
        private readonly Dictionary<int, Dictionary<string, List<Ulica>>> _uliceDict;
        private readonly Dictionary<int, List<UlicaCached>> _uliceCachedDict;
        private readonly StreetMatcher _streetMatcher;
        public readonly PostalCodesLogger _PostalCodesLogger;
        private readonly PostalCodesLogger _fuzzyLogger;
        private readonly PostalCodesLogger _errorLogger;

        public int CorrectedCount { get; private set; }
        public int AmbiguousCount { get; private set; }

        public UlicaMatcher(
            Dictionary<int, Dictionary<string, List<Ulica>>> uliceDict, 
            PostalCodesLogger PostalCodesLogger,
            PostalCodesLogger fuzzyLogger,
            PostalCodesLogger errorLogger,
            HashSet<string> personalStreets)
        {
            _uliceDict = uliceDict;
            _PostalCodesLogger = PostalCodesLogger;
            _fuzzyLogger = fuzzyLogger;
            _errorLogger = errorLogger;
            
            // Konwertuj na UlicaCached
            _uliceCachedDict = ConvertToUlicaCachedDict(uliceDict);
            
            // Inicjalizuj StreetMatcher
            _streetMatcher = new StreetMatcher(personalStreets);
        }

        /// <summary>
        /// Konwertuje słownik Ulica na słownik UlicaCached
        /// </summary>
        private Dictionary<int, List<UlicaCached>> ConvertToUlicaCachedDict(
            Dictionary<int, Dictionary<string, List<Ulica>>> uliceDict)
        {
            var result = new Dictionary<int, List<UlicaCached>>();

            foreach (var kvp in uliceDict)
            {
                var miastoId = kvp.Key;
                var uliceByName = kvp.Value;

                var cachedList = new List<UlicaCached>();

                foreach (var uliceList in uliceByName.Values)
                {
                    foreach (var ulica in uliceList)
                    {
                        var cached = new UlicaCached
                        {
                            Id = ulica.Id,
                            MiastoId = ulica.MiastoId,
                            Cecha = ulica.Cecha,
                            Nazwa1 = ulica.Nazwa1,
                            Nazwa2 = ulica.Nazwa2,
                            Miasto = ulica.Miasto,
                            Dzielnica = ulica.Dzielnica,
                            NormalizedNazwa1 = TextNormalizer.Normalize(ulica.Nazwa1),
                            NormalizedCombined = string.IsNullOrEmpty(ulica.Nazwa2)
                                ? null
                                : TextNormalizer.Normalize($"{ulica.Nazwa2} {ulica.Nazwa1}")
                        };
                        cachedList.Add(cached);
                    }
                }

                result[miastoId] = cachedList;
            }

            return result;
        }

        /// <summary>
        /// Używa StreetMatcher.FindStreet do wyszukiwania ulic
        /// </summary>
        public (Ulica? ulica, string ulicaNazwa) Match(
            string kodPocztowy,
            string sWojewodztwo,
            string sPowiat,
            string sGmina,
            Miasto miasto,
            string sDzielnica,
            string sPrefiks,
            string sUlica
        )
        {
            if (string.IsNullOrEmpty(sUlica))
            {
                return (null, sUlica);
            }

            var currentUlica = sUlica;
            var currentDzielnica = "";

            if (miasto.Nazwa == "Warszawa" && sDzielnica == "Wesoła")
            {
                currentDzielnica = sDzielnica;
            }
            (currentUlica, currentDzielnica) = UliceUtils.ZielonaGora(miasto, currentUlica, currentDzielnica);

            currentUlica = TitleManager.RemoveTitles(currentUlica);

            // KROK 1: Sprawdź czy miejscowość ma jakiekolwiek ulice
            if (!_uliceCachedDict.TryGetValue(miasto.Id, out var uliceCachedList))
            {
                return (null, currentUlica);
            }

            // Filtruj po dzielnicy (jeśli podana)
            var filteredUlice = string.IsNullOrEmpty(currentDzielnica)
                ? uliceCachedList
                : uliceCachedList.Where(u => u.Dzielnica == currentDzielnica).ToList();

            if (filteredUlice.Count == 0)
            {
                return (null, currentUlica);
            }

            // Deleguj wyszukiwanie do StreetMatcher.FindStreet
            var ulicaCached = _streetMatcher.FindStreet(filteredUlice, currentUlica);

            if (ulicaCached == null)
            {
                return (null, currentUlica);
            }

            // Konwertuj UlicaCached z powrotem na Ulica
            var ulica = new Ulica
            {
                Id = ulicaCached.Id,
                MiastoId = ulicaCached.MiastoId,
                Cecha = ulicaCached.Cecha,
                Miasto = ulicaCached.Miasto,
                Dzielnica = ulicaCached.Dzielnica
            };

            // Sprawdź czy to było fuzzy matching
            var normalizedSearch = TextNormalizer.Normalize(currentUlica);
            var wasExactMatch = ulicaCached.NormalizedNazwa1 == normalizedSearch ||
                               (ulicaCached.NormalizedCombined != null && ulicaCached.NormalizedCombined == normalizedSearch);

            if (!wasExactMatch)
            {
                // ✅ POPRAWKA: Użyj ulicaCached.Cecha, Nazwa1, Nazwa2 zamiast computed properties z ulica
                var pelnaNazwa = string.IsNullOrEmpty(ulicaCached.Cecha)
                    ? $"{ulicaCached.Nazwa1}{(string.IsNullOrEmpty(ulicaCached.Nazwa2) ? "" : $" ({ulicaCached.Nazwa2})")}"
                    : $"{ulicaCached.Cecha} {ulicaCached.Nazwa1}{(string.IsNullOrEmpty(ulicaCached.Nazwa2) ? "" : $" ({ulicaCached.Nazwa2})")}_";

                var fuzzyMessage = $"[UlicaMatcher] ✓ FUZZY: Kod={kodPocztowy} | Miejscowość={miasto.Nazwa} | Szukano='{currentUlica}' | Znaleziono='{pelnaNazwa}'";
                
                _PostalCodesLogger.LogInfo(fuzzyMessage);
                _fuzzyLogger.LogInfo(fuzzyMessage);
            }

            return (ulica, currentUlica);
        }

        /// <summary>
        /// Generuje diagnostyczny komunikat o braku ulicy
        /// </summary>
        public string GetNotFoundMessage(string ulicaNazwa, Miasto miasto, string miastoNazwa, string sKorekcja)
        {
            var miastoInfo = $"{miastoNazwa} (MiastoId={miasto.Id})";
            var uliceCountInfo = _uliceDict.ContainsKey(miasto.Id)
                ? $"{_uliceDict[miasto.Id].Count} ulic w słowniku"
                : "brak ulic w słowniku";

            var message = "";

            message = $" Próbowano korekty: '{sKorekcja}'";
            message += $" Nie znaleziono ulicy: '{ulicaNazwa}' w {miastoInfo} ({uliceCountInfo})";

            return message;
        }
    }
}