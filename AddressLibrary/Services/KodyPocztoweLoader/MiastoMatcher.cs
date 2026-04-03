// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Logging;
using AddressLibrary.Models;

namespace AddressLibrary.Services.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje miejscowości w gminach z obsługą korekt
    /// </summary>
    internal class MiastoMatcher
    {
        private readonly Dictionary<string, List<Gmina>> _gminyDict;
        private readonly Dictionary<int, Dictionary<string, Miasto>> _miastaDict;
        private readonly PostalCodesLogger? _logger;
        private readonly PostalCodesLogger? _fuzzyLogger; // ✅ NOWE
        private readonly PostalCodesLogger? _errorLogger; // ✅ NOWE

        public MiastoMatcher(
            Dictionary<string, List<Gmina>> gminyDict,
            Dictionary<int, Dictionary<string, Miasto>> miastaDict,
            PostalCodesLogger? logger,
            PostalCodesLogger? fuzzyLogger,
            PostalCodesLogger? errorLogger) // ✅ NOWE
        {
            _gminyDict = gminyDict;
            _miastaDict = miastaDict;
            _logger = logger;
            _fuzzyLogger = fuzzyLogger;
            _errorLogger = errorLogger; // ✅ NOWE
        }

        /// <summary>
        /// Próbuje znaleźć miejscowość w odpowiedniej gminie
        /// </summary>
        public (Miasto? miasto, Gmina? gmina, string miastoNazwa, string gminaNazwa, int gminyCount) Match(
            Pna pna,
            out bool isMultipleGmin)
        {
            isMultipleGmin = false;
            var currentMiasto = pna.Miasto;
            var currentDzielnica = pna.Dzielnica;
            var currentGmina = pna.Gmina;
            var currentUlica = pna.Ulica;

            // Znajdź gminę
            var gminaKey = $"{pna.Wojewodztwo}|{pna.Powiat}|{currentGmina}".ToLowerInvariant();

            if (!_gminyDict.TryGetValue(gminaKey, out var gminyList))
            {
                // Nie znaleziono gminy - zwróć null
                return (null, null, currentMiasto, currentGmina, 0);
            }

            int gminyCount = gminyList.Count;

            Miasto? miasto = null;
            Gmina? foundGmina = null;
            Miasto? foundMiasto = null;

            // KROK 3: Próbuj znaleźć miasto w każdej gminie
            var foundGminy = new List<(Gmina gmina, Miasto miasto)>(); // ✅ ZMIANA: Przechowuj również miasto
            
            foreach (var gmina in gminyList)
            {
                if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                {
                    // Próba dokładnego dopasowania (case-insensitive) miasta, jeśli istnieje
                    if (miasta.TryGetValue(currentMiasto.ToLowerInvariant(), out miasto))
                    {
                        // ✅ DIAGNOSTYKA: Loguj WSZYSTKIE znalezione miejscowości
                       // _logger?.LogInfo($"[MiastoMatcher] Znaleziono: '{miasto.Nazwa}' (Id={miasto.Id}, RodzajMiasta={miasto.RodzajMiasta}) dla kodu {pna.Kod}");
                        
                        // ✅ NOWE: Odrzuć dzielnice (RodzajMiasta == "99")
                        if (miasto.RodzajMiasta.Kod != "99")
                        {
                            foundGminy.Add((gmina, miasto)); // ✅ Przechowuj gminę I miasto
                          //  _logger?.LogInfo($"[MiastoMatcher] ✓ Akceptuję (nie dzielnica): '{miasto.Nazwa}' (Id={miasto.Id}, Rodzaj={miasto.RodzajMiasta})");
                        }
                        else
                        {
                          //  _logger?.LogInfo($"[MiastoMatcher] ✗ Odrzucam (dzielnica 99): '{miasto.Nazwa}' (Id={miasto.Id})");
                        }
                    }
                }
            }

            // Nie znaleziono - zwróć pierwszą gminę jako kontekst
            if (foundGminy.Count == 0)
            {
                // ✅ DIAGNOSTYKA: Zaloguj dlaczego nie znaleziono
                _errorLogger?.LogError($"[MiastoMatcher] ✗ Nie znaleziono ŻADNEGO miasta '{currentMiasto}' (po odfiltrowaniu dzielnic) dla kodu {pna.Kod}");
                return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);
            }

            // ✅ NOWE: Jeśli znaleziono wiele, PREFERUJ miasto główne (RodzajMiasta="96")
            Miasto? selectedMiasto = null;
            Gmina? selectedGmina = null;

            if (foundGminy.Count > 1)
            {
                // Filtruj tylko miasta główne (96)
                var mainCities = foundGminy.Where(x => x.miasto.RodzajMiasta.Kod == "96").ToList();

                if (mainCities.Count == 1)
                {
                    // ✅ Znaleziono dokładnie jedno miasto główne - wybierz je
                    selectedGmina = mainCities[0].gmina;
                    selectedMiasto = mainCities[0].miasto;
                    _logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano miasto główne (96): '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                }
                else if (mainCities.Count > 1)
                {
                    // ⚠️ Wiele miast głównych - zaloguj błąd i wybierz pierwsze
                    _errorLogger?.LogError($"[MiastoMatcher] ⚠️ Znaleziono {mainCities.Count} miast głównych '{currentMiasto}' dla kodu {pna.Kod}");
                    selectedGmina = mainCities[0].gmina;
                    selectedMiasto = mainCities[0].miasto;
                }
                else
                {
                    // Brak miast głównych - wybierz pierwszą z foundGminy
                    selectedGmina = foundGminy[0].gmina;
                    selectedMiasto = foundGminy[0].miasto;
                    _logger?.LogInfo($"[MiastoMatcher] ✓ Brak miasta głównego, wybrano: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id}, Rodzaj={selectedMiasto.RodzajMiasta})");
                }
            }
            else
            {
                // Tylko jedna miejscowość - wybierz ją
                selectedGmina = foundGminy[0].gmina;
                selectedMiasto = foundGminy[0].miasto;
            }

            foundGmina = selectedGmina;
            miasto = selectedMiasto;

            var powiatKod = gminyList.First().Powiat.Kod;
            var isCityWithPowiatRights = powiatKod.EndsWith("61") || powiatKod.EndsWith("62") ||
                                         powiatKod.EndsWith("63") || powiatKod.EndsWith("64") ||
                                         powiatKod.EndsWith("65");

            // Jeśli dzielnica jest pusta lub jest to miasto powiatowe zwracamy miasto, nie patrząc na dzielnice
            if (string.IsNullOrEmpty(currentDzielnica) || isCityWithPowiatRights) 
                return (miasto, foundGmina, currentMiasto, currentGmina, gminyCount);

            // KROK 4: Próbuj znaleźć dzielnicę w każdej gminie
            // tylko dla miast z niepustą dzielnicą
            if (!string.IsNullOrEmpty(currentDzielnica))
                foreach (var gmina in gminyList)
                {
                    if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                    {
                        // Próba dokładnego dopasowania (case-insensitive) dzielnicy, jeśli istnieje
                        if (miasta.TryGetValue(currentDzielnica.ToLowerInvariant(), out var miasto2))
                        {
                            if (string.IsNullOrEmpty(pna.Ulica) || miasto2.Ulice.Any())
                            {
                                // zwróć dzielnicę
                                return (miasto2, gmina, currentDzielnica, currentGmina, gminyCount);
                            }
                            else
                            {
                                // zwróć miasto
                                return (miasto, foundGmina, currentMiasto, currentGmina, gminyCount);
                            }
                        }
                    }
                }
            // Nie znaleziono - zwróć pierwszą gminę jako kontekst
            return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);
        }
    }

}
