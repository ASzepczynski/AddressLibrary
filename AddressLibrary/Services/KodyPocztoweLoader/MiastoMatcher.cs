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
        public List<Miasto>? Match(
            Pna pna)
        {
            var currentMiasto = pna.Miasto;
            var currentDzielnica = pna.Dzielnica;
            var currentGmina = pna.Gmina;
            var currentUlica = pna.Ulica;

            if (pna.Miasto == "Kraśnik" && pna.Gmina=="Kraśnik")
            {
                int y = 1;
            }
            // Znajdź gminę
            var gminaKey = $"{pna.Wojewodztwo}|{pna.Powiat}|{currentGmina}".ToLowerInvariant();

            if (!_gminyDict.TryGetValue(gminaKey, out var gminyList))
            {
                // Nie znaleziono gminy - zwróć null
                return null;
            }

            int gminyCount = gminyList.Count;


            // KROK 3: Próbuj znaleźć miasto w każdej gminie
            var foundMiasta = new List<Miasto>();

            foreach (var gmina in gminyList)
            {
                if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                {
                    if (miasta.TryGetValue(currentMiasto.ToLowerInvariant(), out var miastaCandidates))
                    {
                        foreach (var candidate in miastaCandidates)
                        {
                            if (candidate.RodzajMiasta.Kod != "99")
                                foundMiasta.Add(candidate);
                        }
                    }
                }
            }

       
            if (foundMiasta.Count == 0)
            {
                // ✅ DIAGNOSTYKA: Zaloguj dlaczego nie znaleziono
                _errorLogger?.LogError($"[MiastoMatcher] ✗ Nie znaleziono ŻADNEGO miasta '{currentMiasto}' (po odfiltrowaniu dzielnic) dla kodu {pna.Kod}");
                return null;
            }

            if (foundMiasta.Count > 1)
            {
                foundMiasta = DuplikatMiasta(_logger, pna.Kod, foundMiasta);
            }

            var powiatKod = gminyList.First().Powiat.Kod;
            var isCityWithPowiatRights = powiatKod.EndsWith("61") || powiatKod.EndsWith("62") ||
                                         powiatKod.EndsWith("63") || powiatKod.EndsWith("64") ||
                                         powiatKod.EndsWith("65");

            // Jeśli dzielnica jest pusta lub jest to miasto powiatowe zwracamy miasto, nie patrząc na dzielnice
            if (string.IsNullOrEmpty(currentDzielnica) || isCityWithPowiatRights)
                return foundMiasta;

            // KROK 4: Próbuj znaleźć dzielnicę w każdej gminie
            // tylko dla miast z niepustą dzielnicą
            if (!string.IsNullOrEmpty(currentDzielnica))
                foreach (var gmina in gminyList)
                {
                    if (_miastaDict.TryGetValue(gmina.Id, out var miasta))
                    {
                        if (miasta.TryGetValue(currentDzielnica.ToLowerInvariant(), out var dzielnicaCandidates))
                        {
                            if (dzielnicaCandidates.Count > 0)
                            {
                                if (string.IsNullOrEmpty(pna.Ulica) || dzielnicaCandidates[0].Ulice.Any())
                                    return dzielnicaCandidates;
                            }
                        }
                    }
                }
            return foundMiasta;
        }

        public List<Miasto>? DuplikatMiasta(
            PostalCodesLogger? logger,
            string kod,
            List<Miasto> foundMiasta)
        {

            if (kod == "23-200" || kod== "23-204")
            {
                // Szukamy Kraśnika i Kraśnika Fabrycznego
                // Przez tą linijkę nie dostawi się niestety kod do Kraśnik(osada)
                var elem = foundMiasta.Where(x => x.Nazwa.StartsWith("Kraśnik") && x.RodzajMiasta.Kod=="96").ToList();
                if(elem.Count==1)return elem;
            }


            if (kod == "26-220")
            {
                // To jest pierwszy Małachów w gminie Końskie
                var elem = foundMiasta.Where(x => x.Kod == "0243760").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Małachów 1/Końskie: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            if (kod == "26-200")
            {
                // To jest drugi Małachów w gminie Końskie
                var elem = foundMiasta.Where(x => x.Kod == "0244340").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Małachów 2/Końskie: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            if (kod == "37-500")
            {
                // To jest Wietlin (osada) w gminie Laszki
                var elem = foundMiasta.Where(x => x.Kod == "0989650").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Wietlin (osada)/Laszki: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            if (kod == "37-512")
            {
                // To jest Wietlin (wieś) w gminie Laszki
                var elem = foundMiasta.Where(x => x.Kod == "0605766").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Wietlin (wieś)/Laszki: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            if (kod == "89-608")
            {
                // To jest Kamionka 1 w chojnickim
                var elem = foundMiasta.Where(x => x.Kod == "0082328").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Kamionkę 1 w chojnickim: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            if (kod == "89-620")
            {
                // To jest Kamionka 2 w chojnickim
                var elem = foundMiasta.Where(x => x.Kod == "0081524").ToList();
                if (elem.Count() == 1)
                {
                    logger?.LogInfo($"[MiastoMatcher] ✓ Wybrano Kamionkę 2 w chojnickim: '{elem[0].Nazwa}' (Id={elem[0].Id})");
                    return elem;
                }
            }

            // Istnieje wiele miast
            return foundMiasta;
        }
    }
}

