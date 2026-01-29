// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using AddressLibrary.Structures;
using System.Collections.Immutable;
using AddressLibrary.Helpers;


namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje ulice w miejscowościach z obsługą korekt
    /// </summary>
    internal class UlicaMatcher
    {
        private readonly Dictionary<int, Dictionary<string, List<Ulica>>> _uliceDict;
        private readonly LoadLogger _loadLogger;

        public int CorrectedCount { get; private set; }
        public int AmbiguousCount { get; private set; } // 🆕 Licznik niejednoznaczności

        public UlicaMatcher(Dictionary<int, Dictionary<string, List<Ulica>>> uliceDict, LoadLogger loadLogger)
        {
            _uliceDict = uliceDict;
            _loadLogger = loadLogger;
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
                    Console.WriteLine($"[UlicaMatcher] ✓ Znaleziono dokładnie jedną ulicę: '{GetPelnaNazwa(ulica)}'");
                }
                else if (exactMatches.Count > 1)
                {
                    // ⚠️ Wiele ulic - NIEJEDNOZNACZNOŚĆ
                    AmbiguousCount++;
                    Console.WriteLine($"[UlicaMatcher] ⚠️ NIEJEDNOZNACZNOŚĆ: Znaleziono {exactMatches.Count} ulic pasujących do '{currentUlica}':");

                    foreach (var match in exactMatches)
                    {
                        Console.WriteLine($"    - ID={match.Id}: '{GetPelnaNazwa(match)}'");
                    }

                    // 🆕 Próba rozstrzygnięcia niejednoznaczności
                    ulica = ResolveAmbiguity(exactMatches, kodPocztowy, miasto.Nazwa);

                    if (ulica != null)
                    {
                        Console.WriteLine($"[UlicaMatcher] ✓ Rozstrzygnięto: wybrano '{GetPelnaNazwa(ulica)}' na podstawie kodu {kodPocztowy}");
                        ulicaFound = true;
                    }
                    else
                    {
                        Console.WriteLine($"[UlicaMatcher] ✗ Nie udało się rozstrzygnąć niejednoznaczności");
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
                        Console.WriteLine($"[UlicaMatcher] ✓ Fuzzy matching znalazł: '{GetPelnaNazwa(ulica)}' w '{ulica.Miasto.Nazwa}'");
                    }
                }
            }

            // KROK 2: Jeśli nie znaleziono ulicy, ZAWSZE spróbuj korekty
            if (!ulicaFound)
            {
                var correctedUlica = KorektyUlic.Popraw(currentUlica, miasto.Nazwa, kodPocztowy);

                // KROK 2a: Sprawdź czy korekta zwróciła inną nazwę
                if (correctedUlica != currentUlica)
                {
                    // KROK 2b: Spróbuj znaleźć skorygowaną ulicę
                    if (_uliceDict.TryGetValue(miasto.Id, out var ulice2))
                    {
                        if (ulice2.TryGetValue(correctedUlica.ToLowerInvariant(), out var lUlica) && lUlica.Count==1)
                        {
                            currentUlica = correctedUlica;
                            CorrectedCount++;
                            ulicaFound = true;
                        }
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
        /// 🆕 Próbuje rozstrzygnąć niejednoznaczność na podstawie kodu pocztowego
        /// </summary>
        private Ulica? ResolveAmbiguity(List<Ulica> candidates, string kodPocztowy, string miastoNazwa)
        {
            if (candidates.Count <= 1)
                return candidates.FirstOrDefault();

           // STRATEGIA 1: Lista cech w kolejności priorytetu
            var cechyPriorytet = new[] { "ul.", "Al.", "Pl." };
            
            foreach (var cecha in cechyPriorytet)
            {
                var matches = candidates.Where(u => u.Cecha == cecha).ToList();
                var pominieteCechy= candidates.Where(u => u.Cecha != cecha).Select(x=>x.Cecha).ToList();
                if (matches.Count == 1)
                {
                    _loadLogger.LogError($"[UlicaMatcher] ✓ Wybrano cechę {kodPocztowy} {miastoNazwa} '{cecha}': '{GetPelnaNazwa(matches[0])}'");
                    if (pominieteCechy.Count > 0)
                    {
                        _loadLogger.LogError($"[UlicaMatcher] Pominięto cechy: {string.Join(", ", pominieteCechy)}");
                    }
                    return matches[0];
                }
                            }

            _loadLogger.LogError($"[UlicaMatcher] ✗ Nie można rozstrzygnąć - zwracam null");
            return null;
        }

        /// <summary>
        /// Generuje diagnostyczny komunikat o braku ulicy
        /// </summary>
        public string GetNotFoundMessage(string ulicaNazwa, Miasto miasto, string miastoNazwa, string correctedUlica)
        {
            var miastoInfo = $"{miastoNazwa} (MiastoId={miasto.Id})";
            var uliceCountInfo = _uliceDict.ContainsKey(miasto.Id)
                ? $"{_uliceDict[miasto.Id].Count} ulic w słowniku"
                : "brak ulic w słowniku";

            var message = $"Nie znaleziono ulicy: '{ulicaNazwa}' w {miastoInfo} ({uliceCountInfo})";

            if (correctedUlica != ulicaNazwa)
            {
                message += $" | Próbowano korekty: '{correctedUlica}'";
            }

            return message;
        }

        /// <summary>
        /// Buduje pełną nazwę ulicy z Nazwa2 (prefiks) + Nazwa1 (główna nazwa)
        /// </summary>
        private static string GetPelnaNazwa(Ulica ulica)
        {
            if (string.IsNullOrEmpty(ulica.Nazwa2))
            {
                return ulica.Nazwa1;
            }
            return $"{ulica.Nazwa2} {ulica.Nazwa1}";
        }
    }
}