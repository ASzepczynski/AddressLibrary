// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Models;


namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Wyszukuje ulice w miejscowoœciach z obs³ug¹ korekt
    /// </summary>
    internal class UlicaMatcher
    {
        private readonly Dictionary<int, Dictionary<string, Ulica>> _uliceDict;

        public int CorrectedCount { get; private set; }

        public UlicaMatcher(Dictionary<int, Dictionary<string, Ulica>> uliceDict)
        {
            _uliceDict = uliceDict;
        }

        /// <summary>
        /// Próbuje znaleŸæ ulicê w danej miejscowoœci
        /// </summary>
        public (Ulica? ulica, string ulicaNazwa) Match(
            string ulicaNazwa,
            Miejscowosc miejscowosc,
            string miastoNazwa,
            string kodPocztowy)
        {
            if (string.IsNullOrEmpty(ulicaNazwa))
            {
                return (null, ulicaNazwa);
            }

            var currentUlica = ulicaNazwa;
            Ulica? ulica = null;
            bool ulicaFound = false;

            // KROK 1: SprawdŸ czy miejscowoœæ ma jakiekolwiek ulice
            if (_uliceDict.TryGetValue(miejscowosc.Id, out var ulice))
            {
                // KROK 1a: Próba dok³adnego dopasowania (case-insensitive)
                if (ulice.TryGetValue(currentUlica.ToLowerInvariant(), out ulica))
                {
                    ulicaFound = true;
                }
                // KROK 1b: Próba rozszerzonego dopasowania (podobieñstwo >= 70%)
                else if (ulice.TryGetValueAgain(currentUlica, out ulica))
                {
                    ulicaFound = true;
                }
            }

            // KROK 2: Jeœli nie znaleziono ulicy, ZAWSZE spróbuj korekty
            if (!ulicaFound)
            {
                var correctedUlica = KorektyUlic.Popraw(ulicaNazwa, miastoNazwa, kodPocztowy);

                // KROK 2a: SprawdŸ czy korekta zwróci³a inn¹ nazwê
                if (correctedUlica != ulicaNazwa)
                {
                    // KROK 2b: Spróbuj znaleŸæ skorygowan¹ ulicê
                    if (_uliceDict.TryGetValue(miejscowosc.Id, out var ulice2))
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
        /// Generuje diagnostyczny komunikat o braku ulicy
        /// </summary>
        public string GetNotFoundMessage(string ulicaNazwa, Miejscowosc miejscowosc, string miastoNazwa, string correctedUlica)
        {
            var miejscowoscInfo = $"{miastoNazwa} (MiejscowoscId={miejscowosc.Id})";
            var uliceCountInfo = _uliceDict.ContainsKey(miejscowosc.Id)
                ? $"{_uliceDict[miejscowosc.Id].Count} ulic w s³owniku"
                : "brak ulic w s³owniku";

            var message = $"Nie znaleziono ulicy: '{ulicaNazwa}' w {miejscowoscInfo} ({uliceCountInfo})";
            
            if (correctedUlica != ulicaNazwa)
            {
                message += $" | Próbowano korekty: '{correctedUlica}'";
            }

            return message;
        }
    }
}