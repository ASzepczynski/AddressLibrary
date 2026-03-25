// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
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
            StreetParser streetParser)
        {
            _uliceDict = uliceDict;
            _PostalCodesLogger = PostalCodesLogger;
            _fuzzyLogger = fuzzyLogger;
            _errorLogger = errorLogger;
            
            // Konwertuj na UlicaCached
            _uliceCachedDict = ConvertToUlicaCachedDict(uliceDict);
            
            // Inicjalizuj StreetMatcher z parserem
            _streetMatcher = new StreetMatcher(streetParser);
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
                            Cecha = ulica.Cecha ?? "",
                            Miasto = ulica.Miasto,
                            Dzielnica = ulica.Dzielnica,
                            TypUlicyId = ulica.TypUlicyId,
                            
                            // 🚀 Pre-normalizuj komponenty z TypUlicy
                            // ✅ POPRAWKA: Sprawdzaj TypUlicyId != -1 zamiast null
                            Prefiks = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Prefiks)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Prefiks),
                            
                            // ✅ POPRAWKA: Sprawdzaj TytulStopienId != -1
                            Tytul = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || ulica.TypUlicy.TytulStopienId == -1 || ulica.TypUlicy.TytulStopien == null
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.TytulStopien.Dopelniacz ?? ulica.TypUlicy.TytulStopien.Skrot ?? ""),
                            
                            Imie = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Imie)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Imie),
                            
                            Imie2 = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Imie2)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Imie2),
                            
                            Nazwisko = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Nazwisko)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Nazwisko),
                            
                            Nazwisko2 = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Nazwisko2)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Nazwisko2),
                            
                            Pseudonim = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Pseudonim)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Pseudonim),
                            
                            Postfiks = ulica.TypUlicyId == -1 || ulica.TypUlicy == null || string.IsNullOrWhiteSpace(ulica.TypUlicy.Postfiks)
                                ? string.Empty
                                : TextNormalizer.Normalize(ulica.TypUlicy.Postfiks)
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
                // ✅ POPRAWKA: Zdefiniuj normalizedSearch
                var normalizedSearch = TextNormalizer.Normalize(currentUlica);
                
                // ✅ DIAGNOSTYKA: Wypisz TOP 20 najbliższych dopasowań z score'ami
                var allWithScores = _streetMatcher.FindAllWithScores(filteredUlice, currentUlica);
                
                _errorLogger.LogError($"[DIAGNOSTYKA] Nie znaleziono ulicy '{currentUlica}' w {miasto.Nazwa}");
                _errorLogger.LogError($"[DIAGNOSTYKA] Normalized search: '{normalizedSearch}'");
                
                if (allWithScores.Count > 0 && allWithScores[0].parsed != null)
                {
                    var p = allWithScores[0].parsed;
                    _errorLogger.LogError($"[DIAGNOSTYKA] Parsed search: " +
                        $"C='{p.Cecha ?? "n"}', " +
                        $"Pr='{p.Prefiks ?? "n"}', " +
                        $"Tyt='{p.Tytul ?? "n"}', " +
                        $"I='{p.Imie ?? "n"}', " +
                        $"I2='{p.Imie2 ?? "n"}', " +
                        $"N='{p.Nazwisko ?? "n"}', " +
                        $"N2='{p.Nazwisko2 ?? "n"}', " +
                        $"Ps='{p.Pseudonim ?? "n"}'");
                }
                
                //_errorLogger.LogError($"[DIAGNOSTYKA] TOP 3 najbliższych dopasowań:");
                
                //foreach (var (u, score, reason, parsed) in allWithScores.Take(3))
                //{
                //    var displayName = u.GetDisplayName();
                //    var fullNorm = u.GetFullNormalized();
                    
                //    _errorLogger.LogError($"  [{score:000}] ID:{u.Id} | '{displayName}' | norm:'{fullNorm}' | {reason}");
                //    _errorLogger.LogError($"Ulica: " +
                //        $"C='{u.Cecha ?? "n"}', " +
                //        $"Pr='{u.Prefiks ?? "n"}', " +
                //        $"Tyt='{u.Tytul ?? "n"}', " +
                //        $"I='{u.Imie ?? "n"}', " +
                //        $"I2='{u.Imie2 ?? "n"}', " +
                //        $"N='{u.Nazwisko ?? "n"}', " +
                //        $"N2='{u.Nazwisko2 ?? "n"}', " +
                //        $"Ps='{u.Pseudonim ?? "n"}'");

                //}

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

            // Loguj matching
            var matchMessage = $"[UlicaMatcher] ✓ MATCHED: Kod={kodPocztowy} | Miejscowość={miasto.Nazwa} | Szukano='{currentUlica}' | Znaleziono='{ulicaCached.GetDisplayName()}'";
            
            _PostalCodesLogger.LogInfo(matchMessage);
            _fuzzyLogger.LogInfo(matchMessage);

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