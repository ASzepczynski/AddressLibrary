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
        private readonly Dictionary<int, Dictionary<string, Ulica>> _uliceDict;

        public int CorrectedCount { get; private set; }
        public int AmbiguousCount { get; private set; } // 🆕 Licznik niejednoznaczności

        public UlicaMatcher(Dictionary<int, Dictionary<string, Ulica>> uliceDict)
        {
            _uliceDict = uliceDict;
        }

        /// <summary>
        /// Próbuje znaleźć ulicę w danej miejscowości
        /// </summary>
        public (Ulica? ulica, string ulicaNazwa) Match(
            string ulicaNazwa,
            string dzielnicaNazwa,
            Miasto miasto,
            string miastoNazwa,
            string kodPocztowy)
        {
            if (string.IsNullOrEmpty(ulicaNazwa))
            {
                return (null, ulicaNazwa);
            }

            var currentUlica = ulicaNazwa;
            var currentDzielnica = dzielnicaNazwa;
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

                    // 🆕 Próba rozstrzygnięcia po kodzie pocztowym
                    ulica = ResolveAmbiguity(exactMatches, kodPocztowy, miastoNazwa);
                    
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
                        Console.WriteLine($"[UlicaMatcher] ✓ Fuzzy matching znalazł: '{GetPelnaNazwa(ulica)}'");
                    }
                }
            }

            // KROK 2: Jeśli nie znaleziono ulicy, ZAWSZE spróbuj korekty
            if (!ulicaFound)
            {
                var correctedUlica = KorektyUlic.Popraw(ulicaNazwa, miastoNazwa, kodPocztowy);

                // KROK 2a: Sprawdź czy korekta zwróciła inną nazwę
                if (correctedUlica != ulicaNazwa)
                {
                    // KROK 2b: Spróbuj znaleźć skorygowaną ulicę
                    if (_uliceDict.TryGetValue(miasto.Id, out var ulice2))
                    {
                        if (ulice2.TryGetValue(correctedUlica.ToLowerInvariant(), out ulica))
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
        private List<Ulica> FindAllExactMatches(Miasto miasto, Dictionary<string, Ulica> ulice, string ulicaNazwa, string dzielnicaNazwa)
        {
            
            var matches = new List<Ulica>();
            var ulic = new ResultList();
            
            ulic.Ulica = new TerytUlic();
            ulic.Ulica.Nazwa1 = ulicaNazwa;
            ulic.Ulica.Nazwa2 = "";
            ulic.Ulica.Cecha = "";
            
            ulic.WojewodztwoNazwa = miasto.Gmina.Powiat.Wojewodztwo.Nazwa;
            ulic.PowiatNazwa = miasto.Gmina.Powiat.Nazwa;
            ulic.GminaNazwa = miasto.Gmina.Nazwa;

            ulic.Miasto = new TerytSimc();
            ulic.Miasto.Nazwa=miasto.Nazwa;
            ulic.Miasto.RodzajMiasta = miasto.RodzajMiasta.Kod;

            string Nazwa1;
            string dzielnica;
            (Nazwa1, dzielnica) = UliceUtils.ZielonaGoraWesola(ulic);

            if (dzielnicaNazwa != "")
            {
                dzielnica = dzielnicaNazwa;
            }

            var normalizedSearch = ulicaNazwa.ToLowerInvariant();
            
         
            foreach (var kvp in ulice)
            {
                // Klucz słownika jest już znormalizowany (lowercase)
                if (kvp.Key == normalizedSearch) // Tu by wypadało sprawdzić dzielnicę
                {
                    matches.Add(kvp.Value);
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

            Console.WriteLine($"[UlicaMatcher] Próba rozstrzygnięcia dla kodu: {kodPocztowy}");

            // STRATEGIA 1: Ulica z pustym Nazwa2 (krótsza nazwa) ma wyższy priorytet
            // Przykład: "Józefa" (Nazwa2="") > "Księcia Józefa" (Nazwa2="Księcia")
            var withoutPrefix = candidates.Where(u => string.IsNullOrEmpty(u.Nazwa2)).ToList();
            
            if (withoutPrefix.Count == 1)
            {
                Console.WriteLine($"[UlicaMatcher] ✓ Wybrano ulicę bez prefiksu: '{GetPelnaNazwa(withoutPrefix[0])}'");
                return withoutPrefix[0];
            }

            // STRATEGIA 2: TODO - w przyszłości można sprawdzić kody pocztowe
            // if (!string.IsNullOrEmpty(kodPocztowy))
            // {
            //     // Sprawdź która ulica ma kod pocztowy pasujący do tego rekordu
            // }

            Console.WriteLine($"[UlicaMatcher] ✗ Nie można rozstrzygnąć - zwracam null");
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