// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Logging;
using AddressLibrary.Models;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
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
            var currentGmina = pna.Gmina;

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

            // KROK 3: Próbuj znaleźć miasto w każdej gminie
            foreach (var gmina in gminyList)
            {
                if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                {
                    // Próba dokładnego dopasowania (case-insensitive)
                    if (miasta.TryGetValue(currentMiasto.ToLowerInvariant(), out var miasto))
                    {
                        return (miasto, gmina, currentMiasto, currentGmina, gminyCount);
                    }
                }
            }
            // Nie znaleziono - zwróć pierwszą gminę jako kontekst
            return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);
        }
    }
}