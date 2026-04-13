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
        private readonly Dictionary<int, Dictionary<string, List<Miasto>>> _miastaDict;
        private readonly PostalCodesLogger? _logger;
        private readonly PostalCodesLogger? _fuzzyLogger; // ✅ NOWE
        private readonly PostalCodesLogger? _errorLogger; // ✅ NOWE

        public MiastoMatcher(
            Dictionary<string, List<Gmina>> gminyDict,
            Dictionary<int, Dictionary<string, List<Miasto>>> miastaDict,
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
                    if (miasta.TryGetValue(currentMiasto.ToLowerInvariant(), out var miastaCandidates))
                    {
                        foreach (var candidate in miastaCandidates)
                        {
                            if (candidate.RodzajMiasta.Kod != "99")
                                foundGminy.Add((gmina, candidate));
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

            if (foundGminy.Count == 1)
            {
                selectedGmina = foundGminy[0].gmina;
                selectedMiasto = foundGminy[0].miasto;
            }
            else
            {
                DuplikatMiasta(_logger, pna.Kod, foundGminy, out selectedGmina, out selectedMiasto);
                return (selectedMiasto, selectedGmina, selectedMiasto?.Nazwa, selectedGmina?.Nazwa, 1);
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
                        if (miasta.TryGetValue(currentDzielnica.ToLowerInvariant(), out var dzielnicaCandidates))
                        {
                            var miasto2 = dzielnicaCandidates.FirstOrDefault();
                            if (miasto2 != null)
                            {
                                if (string.IsNullOrEmpty(pna.Ulica) || miasto2.Ulice.Any())
                                    return (miasto2, gmina, currentDzielnica, currentGmina, gminyCount);
                                else
                                    return (miasto, foundGmina, currentMiasto, currentGmina, gminyCount);
                            }
                        }
                    }
                }
            // Znaleziono więcej niż 1- zwróć pierwszą gminę jako kontekst
            return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);
        }

        private void DuplikatMiasta(
            PostalCodesLogger? logger,
            string kod,
            List<(Gmina gmina, Miasto miasto)> foundGminy,
            out Gmina? selectedGmina,
            out Miasto? selectedMiasto)
        {
            var mainCities = foundGminy.Where(x => x.miasto.RodzajMiasta.Kod == "96").ToList();

            if (mainCities.Count == 1)
            {
                selectedGmina  = mainCities[0].gmina;
                selectedMiasto = mainCities[0].miasto;
                logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano miasto główne (96): '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                return;
            }

            if (kod == "26-220")
            {
                // To jest pierwszy Małachów w gminie Końskie
                var elem= foundGminy.Where(x => x.miasto.Kod == "0243760").ToList();
                if (elem.Count() == 1)
                {
                    selectedGmina = elem[0].gmina;
                    selectedMiasto = elem[0].miasto;
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Małachów 1/Końskie: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                    return;
                }
            }

            if (kod == "26-200")
            {
                // To jest drugi Małachów w gminie Końskie
                var elem = foundGminy.Where(x => x.miasto.Kod == "0244340").ToList();
                if (elem.Count() == 1)
                {
                    selectedGmina = elem[0].gmina;
                    selectedMiasto = elem[0].miasto;
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Małachów 2/Końskie: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                    return;
                }
            }

            if (kod == "37-500")
            {
                // To jest Wietlin (osada) w gminie Laszki
                var elem = foundGminy.Where(x => x.miasto.Kod == "0989650").ToList();
                if (elem.Count() == 1)
                {
                    selectedGmina = elem[0].gmina;
                    selectedMiasto = elem[0].miasto;
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Wietlin (osada)/Laszki: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                    return;
                }
            }

            if (kod == "37-512")
            {
                // To jest Wietlin (wieś) w gminie Laszki
                var elem = foundGminy.Where(x => x.miasto.Kod == "0605766").ToList();
                if (elem.Count() == 1)
                {
                    selectedGmina = elem[0].gmina;
                    selectedMiasto = elem[0].miasto;
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Wietlin (wieś)/Laszki: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id})");
                    return;
                }
            }

            // Brak miast głównych — wybierz pierwsze z foundGminy
            selectedGmina = foundGminy[0].gmina;
            selectedMiasto = foundGminy[0].miasto;
            logger?.LogInfo($"[MiastoMatcher] ✓ Brak miasta głównego, wybrano: '{selectedMiasto.Nazwa}' (Id={selectedMiasto.Id}, Rodzaj={selectedMiasto.RodzajMiasta})");
        }
    }
}

