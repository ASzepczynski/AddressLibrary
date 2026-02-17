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

        public MiastoMatcher(
            Dictionary<string, List<Gmina>> gminyDict,
            Dictionary<int, Dictionary<string, Miasto>> miastaDict,
            PostalCodesLogger? logger)
        {
            _gminyDict = gminyDict;
            _miastaDict = miastaDict;
            _logger = logger;
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

            if (gminyList.Count > 1)
            {
                isMultipleGmin = true;
            }

            bool found = false;
            Miasto? miasto = null;
            Gmina? foundGmina = null;

            // KROK 3: Próbuj znaleźć miasto w każdej gminie
            foreach (var gmina in gminyList)
            {
                if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                {
                    // Próba dokładnego dopasowania (case-insensitive) miasta, jeśli istnieje
                    if (miasta.TryGetValue(currentMiasto.ToLowerInvariant(), out miasto))
                    {
                        foundGmina = gmina;
                        found = true;
                        break;
                    }
                }
            }
            // Nie znaleziono - zwróć pierwszą gminę jako kontekst
            if (!found) return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);

            var powiatKod = gminyList.First().Powiat.Kod;
            var isCityWithPowiatRights = powiatKod.EndsWith("61") || powiatKod.EndsWith("62") ||
                                         powiatKod.EndsWith("63") || powiatKod.EndsWith("64") ||
                                         powiatKod.EndsWith("65");

            // Jeśi dzielnica jest pusta lub jest to miasto powiatowe zwracamy miasto, nie patrząc na dzielnice
            if (string.IsNullOrEmpty(currentDzielnica) || isCityWithPowiatRights) return (miasto, foundGmina, currentMiasto, currentGmina, gminyCount);

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
                            if (string.IsNullOrEmpty(pna.Ulica) || miasto2.Ulice.Any() ){
                                // zwróć dzielnicę
                                return (miasto2, gmina, currentDzielnica, currentGmina, gminyCount);
                            } else
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
