// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Structures;
using System.Collections.Immutable;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;


namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje ulice w miejscowościach z obsługą korekt
    /// </summary>
    internal class UlicaMatcher
    {
        private readonly Dictionary<int, Dictionary<string, List<Ulica>>> _uliceDict;
        public readonly PostalCodesLogger _PostalCodesLogger;

        public int CorrectedCount { get; private set; }
        public int AmbiguousCount { get; private set; } // 🆕 Licznik niejednoznaczności

        public UlicaMatcher(Dictionary<int, Dictionary<string, List<Ulica>>> uliceDict, PostalCodesLogger PostalCodesLogger)
        {
            _uliceDict = uliceDict;
            _PostalCodesLogger = PostalCodesLogger;
        }

        /// <summary>
        /// Próbuje znaleźć ulicę w danej miejscowości
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

            Ulica? ulica = null;
            bool ulicaFound = false;

            // KROK 1: Sprawdź czy miejscowość ma jakiekolwiek ulice
            if (_uliceDict.TryGetValue(miasto.Id, out var ulice))
            {

                // 🆕 KROK 1a: Znajdź WSZYSTKIE dokładnie pasujące ulice
                var exactMatches = FindAllExactMatches(miasto, ulice, currentUlica, currentDzielnica);

                if (exactMatches.Count == 1)
                {
                    // ✅ Dokładnie jedna ulica - OK
                    ulica = exactMatches[0];
                    ulicaFound = true;
//                    _PostalCodesLogger.LogInfo($"[UlicaMatcher] ✓ Znaleziono dokładnie jedną ulicę: '{UliceUtils.GetPelnaNazwa(ulica)}'");
                }
                else if (exactMatches.Count > 1)
                {
                    // ⚠️ Wiele ulic - NIEJEDNOZNACZNOŚĆ
                    AmbiguousCount++;
                    _PostalCodesLogger.LogWarning($"[UlicaMatcher] ⚠️ NIEJEDNOZNACZNOŚĆ: Znaleziono {exactMatches.Count} ulic pasujących do '{currentUlica}':");

                    // 🆕 Próba rozstrzygnięcia niejednoznaczności
                    if (sPrefiks == "") sPrefiks = "ulica";
                    // Nie podajemy kodu pocztowego, bo właśnie go ładujemy - to jest ładowanie kodów pocztowych
                    ulica = AddressLibrary.Helpers.ResolveAmbiguity.ResolveStreetAmbiguity(
                        exactMatches, 
                        sPrefiks, 
                        currentUlica, 
                        currentDzielnica , 
                        "", 
                        miasto.Nazwa,
                        null,
                        _PostalCodesLogger);

                    if (ulica != null)
                    {
                        _PostalCodesLogger.LogInfo($"[UlicaMatcher] ✓ Rozstrzygnięto: wybrano '{sPrefiks} {UliceUtils.GetPelnaNazwa(ulica)}'");
                        ulicaFound = true;
                    }
                    else
                    {
                        _PostalCodesLogger.LogError($"[UlicaMatcher] ✗ Nie udało się rozstrzygnąć niejednoznaczności");
                        // Zwróć null - błąd zostanie zalogowany
                        return (null, currentUlica);
                    }
                }
                else
                {
                    // KROK 1b: Brak dokładnego dopasowania - spróbuj fuzzy matching
                    if (ulice.TryGetValueAgain(currentUlica, out ulica))
                    {
                        ulicaFound = true;
                        _PostalCodesLogger.LogInfo($"[UlicaMatcher] ✓ Fuzzy matching dla [{currentUlica}] znalazł: '{UliceUtils.GetPelnaNazwa(ulica)}' w '{ulica.Miasto.Nazwa}'");
                    }
                }
            }
            return (ulica, currentUlica);
        }

        /// <summary>
        /// 🆕 Znajduje wszystkie ulice dokładnie pasujące do szukanej nazwy (case-insensitive)
        /// </summary>
        private List<Ulica> FindAllExactMatches(Miasto miasto, Dictionary<string, List<Ulica>> ulice, string sUlica, string sDzielnica)
        {

            var matches = new List<Ulica>();

            var normalizedSearch = sUlica.ToLowerInvariant();

            foreach (var kvp in ulice)
            {
                // Klucz słownika jest już znormalizowany (lowercase)
                if (kvp.Key == normalizedSearch) 
                {
                    foreach (var uliczka in kvp.Value)
                        if (uliczka.Dzielnica == sDzielnica)
                        {
                            matches.Add(uliczka);
                        }
                }
            }

            return matches;
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