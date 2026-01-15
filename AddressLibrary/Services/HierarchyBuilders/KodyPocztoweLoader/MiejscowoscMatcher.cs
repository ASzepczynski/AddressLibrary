// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

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
        private readonly LoadLogger? _logger;

        public int CorrectedCount { get; private set; }

        public MiastoMatcher(
            Dictionary<string, List<Gmina>> gminyDict,
            Dictionary<int, Dictionary<string, Miasto>> miastaDict,
            LoadLogger? logger = null)
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

            // KROK 1: Sprawdź czy jest korekta gminy
            var correctedGmina = KorektyMiasta.PoprawGmina(currentMiasto, currentGmina, pna.Kod);
            if (correctedGmina != currentGmina)
            {
                _logger?.LogError($"✓ KOREKTA GMINY dla kodu {pna.Kod}: '{currentGmina}' → '{correctedGmina}' (miasto: {currentMiasto})");
                currentGmina = correctedGmina;
                CorrectedCount++;
            }

            // KROK 2: Znajdź gminę
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

            // KROK 4: Nie znaleziono - spróbuj korekty
            var correctedMiasto = KorektyMiasta.Popraw(currentMiasto, currentGmina, pna.Powiat, pna.Wojewodztwo, pna.Kod);

            if (correctedMiasto != currentMiasto)
            {
                _logger?.LogError($"✓ KOREKTA MIASTA dla kodu {pna.Kod}: '{currentMiasto}' → '{correctedMiasto}' (gmina: {currentGmina})");
                
                // Spróbuj ponownie z skorygowaną nazwą - TYLKO DOKŁADNE DOPASOWANIE
                foreach (var gmina in gminyList)
                {
                    if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                    {
                        if (miasta.TryGetValue(correctedMiasto.ToLowerInvariant(), out var miasto))
                        {
                            CorrectedCount++;
                            return (miasto, gmina, correctedMiasto, currentGmina, gminyCount);
                        }
                    }
                }

                // Jeśli nadal nie znaleziono
                _logger?.LogError($"⚠️ KOREKTA NIE POMOGŁA dla kodu {pna.Kod}: skorygowano '{currentMiasto}' → '{correctedMiasto}', ale nadal nie znaleziono w gminie '{currentGmina}'");
            }

            // Nie znaleziono - zwróć pierwszą gminę jako kontekst
            return (null, gminyList.First(), currentMiasto, currentGmina, gminyCount);
        }
    }
}