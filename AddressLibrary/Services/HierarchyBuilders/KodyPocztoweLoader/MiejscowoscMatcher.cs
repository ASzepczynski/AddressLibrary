// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje miejscowości w gminach z obsługą korekt
    /// </summary>
    internal class MiejscowoscMatcher
    {
        private readonly Dictionary<string, List<Gmina>> _gminyDict;
        private readonly Dictionary<int, Dictionary<string, Miejscowosc>> _miejscowosciDict;
        private readonly LoadLogger? _logger;

        public int CorrectedCount { get; private set; }

        public MiejscowoscMatcher(
            Dictionary<string, List<Gmina>> gminyDict,
            Dictionary<int, Dictionary<string, Miejscowosc>> miejscowosciDict,
            LoadLogger? logger = null)
        {
            _gminyDict = gminyDict;
            _miejscowosciDict = miejscowosciDict;
            _logger = logger;
        }

        /// <summary>
        /// Próbuje znaleźć miejscowość w odpowiedniej gminie
        /// </summary>
        public (Miejscowosc? miejscowosc, Gmina? gmina, string miasto, string gminaNazwa, int gminyCount) Match(
            Pna pna,
            out bool isMultipleGmin)
        {
            isMultipleGmin = false;
            var currentMiasto = pna.Miasto;
            var currentGmina = pna.Gmina;

            // KROK 1: Sprawdź czy jest korekta gminy
            var correctedGmina = KorektyMiejscowosci.PoprawGmina(currentMiasto, currentGmina, pna.Kod);
            if (correctedGmina != currentGmina)
            {
                _logger?.LogError($"✓ KOREKTA GMINY dla kodu {pna.Kod}: '{currentGmina}' → '{correctedGmina}' (miejscowość: {currentMiasto})");
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

            // KROK 3: Próbuj znaleźć miejscowość w każdej gminie - TYLKO DOKŁADNE DOPASOWANIE
            foreach (var gmina in gminyList)
            {
                if (_miejscowosciDict.TryGetValue(gmina.Id, out var miejscowosci))
                {
                    // Próba dokładnego dopasowania (case-insensitive)
                    if (miejscowosci.TryGetValue(currentMiasto.ToLowerInvariant(), out var miejscowosc))
                    {
                        return (miejscowosc, gmina, currentMiasto, currentGmina, gminyCount);
                    }
                }
            }

            // KROK 4: Nie znaleziono miejscowości - spróbuj korekty miejscowości
            var correctedMiasto = KorektyMiejscowosci.Popraw(currentMiasto, currentGmina, pna.Powiat, pna.Wojewodztwo, pna.Kod);

            if (correctedMiasto != currentMiasto)
            {
                _logger?.LogError($"✓ KOREKTA MIEJSCOWOŚCI dla kodu {pna.Kod}: '{currentMiasto}' → '{correctedMiasto}' (gmina: {currentGmina})");
                
                // Spróbuj ponownie z skorygowaną nazwą - TYLKO DOKŁADNE DOPASOWANIE
                foreach (var gmina in gminyList)
                {
                    if (_miejscowosciDict.TryGetValue(gmina.Id, out var miejscowosci))
                    {
                        if (miejscowosci.TryGetValue(correctedMiasto.ToLowerInvariant(), out var miejscowosc))
                        {
                            CorrectedCount++;
                            return (miejscowosc, gmina, correctedMiasto, currentGmina, gminyCount);
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