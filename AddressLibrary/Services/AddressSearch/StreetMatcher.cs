// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using CsvHelper;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do dopasowywania nazw ulic (strukturalne dopasowanie komponentów)
    /// </summary>
    public class StreetMatcher
    {
        private readonly StreetParser _parser;

        public StreetMatcher(StreetParser parser)
        {
            _parser = parser;
        }

        
        /// <summary>
        /// 🚀 Strukturalne dopasowywanie komponentów ulicy
        /// Znajduje ulicę w liście UlicaCached 
        /// 
        /// </summary>
        public UlicaCached? FindStreet(List<UlicaCached> ulice, string streetName,out bool wasFuzzy)
        {
            wasFuzzy = false;
            if (string.IsNullOrWhiteSpace(streetName))
                return null;
            (string sCecha, string normalizedSearch) = CechyUlicUtils.SplitStreetPrefix(streetName);

            normalizedSearch = TextNormalizer.Normalize(normalizedSearch);

            var parsed = _parser.Parse(normalizedSearch);
        
            var nowaUlica = new UlicaCached
            {
                Prefiks   = parsed.Prefiks,
                Tytul     = parsed.Tytul,
                Imie      = parsed.Imie,
                Imie2     = parsed.Imie2,
                Nazwisko  = parsed.Nazwisko,
                Nazwisko2 = parsed.Nazwisko2,
                Pseudonim = parsed.Pseudonim,
                Postfiks  = parsed.Postfiks,
            };

            var parsedSearch = TextNormalizer.Normalize(nowaUlica.GetShortName());
        
            // Najpierw sprawdzamy wprost - po nazwie
            foreach (var ulica in ulice)
            {

                var normalizedShort = ulica.NormalizedShortName;
                if (normalizedShort.Contains("jozefa"))
                {
                    int y = 1;
                }
                // Zwykłe porównanie nazw

                var isCechaOk = sCecha == "" || sCecha == ulica.CechaUlicy.Skrot || sCecha == ulica.CechaUlicy.Nazwa;
                if (!isCechaOk) continue;

                if (normalizedShort == normalizedSearch)
                    return ulica;
                if (normalizedShort == parsedSearch)
                    return ulica;

                var normalizedFull = ulica.NormalizedFullName;
                // Zwykłe porównanie nazw
                if (normalizedFull == normalizedSearch)
                    return ulica;
                if (normalizedFull == parsedSearch)
                    return ulica;
            }

            // Teraz z wyceną
            UlicaCached? bestMatch = null;
            int bestScore = 0;
            foreach (var ulica in ulice)
            {
                var isCechaOk = sCecha == "" || sCecha == ulica.CechaUlicy.Skrot || sCecha == ulica.CechaUlicy.Nazwa;
                if (!isCechaOk) continue;

                var normalizedShort = ulica.NormalizedShortName;
                if (normalizedShort.Contains("odrow"))
                {
                    int y = 1;
                }
                var score = CalculateMatchScore(parsed, ulica);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = ulica;
                }
            }
            if (bestMatch != null && bestScore > 70) return bestMatch;

            //// Fuzzy matching dla ulic nie-osobowych (jeśli nie znaleziono osobowej)
            //foreach (var ulica in ulice)
            //{
            //    var normalizedFull = ulica.GetFullNormalized();
            //    // Częściowe dopasowanie (contains)
            //    if (normalizedFull.Contains(normalizedSearch) || normalizedSearch.Contains(normalizedFull))
            //    {
            //        int distance = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(normalizedSearch, normalizedFull);
            //        if (distance <= 2)
            //        {
            //            wasFuzzy = true;
            //            return ulica;
            //        }
            //    }
            //}

            return null;
        }

        /// <summary>
        /// Oblicza score dopasowania (0-100) porównując komponenty
        /// </summary>
        private int CalculateMatchScore(ParsedStreet search, UlicaCached ulica)
        {
            int totalWeight = 0;
            int matchedWeight = 0;

            // WAGI komponentów
            const int NAZWISKO_WEIGHT = 50;
            const int IMIE_WEIGHT = 10;
            const int TYTUL_WEIGHT = 5;
            const int PSEUDONIM_WEIGHT = 5;
            const int IMIE2_WEIGHT = 5;

// Dla nieosobowych zwróć zero
            if (ulica.IsEmpty()) return 0;

            // 0. Dla królowej Jadwigi

            if (string.IsNullOrEmpty(search.Nazwisko) && !string.IsNullOrEmpty(search.Imie))
            {
       
                int score = 0;

                if (ulica.Imie == search.Imie)
                {
                    if (ulica.Postfiks == search.Postfiks && ulica.Prefiks == search.Prefiks)
                    {
                        score = 80;
                    }
                    if (TitleManager.TenSamTytul(ulica.Tytul, search.Tytul))
                    {
                        score += TYTUL_WEIGHT;
                    }
                }
                return score;
            }

            // 1. Nazwisko (MUST MATCH dla ulic osobowych!)
            if (!string.IsNullOrEmpty(search.Nazwisko))
            {
                totalWeight += NAZWISKO_WEIGHT;

                if (ulica.Nazwisko == search.Nazwisko)
                {
                    matchedWeight += NAZWISKO_WEIGHT;
                }
                else if (ulica.Nazwisko2 == search.Nazwisko)
                {
                    matchedWeight += NAZWISKO_WEIGHT;
                }
                else
                {
                    // Nazwisko nie pasuje - zwróć 0
                    return 0;
                }
            }

            if (!string.IsNullOrEmpty(search.Pseudonim) && search.Pseudonim==ulica.Pseudonim)
            {
                if (search.Imie == ulica.Imie 
                    && search.Nazwisko == ulica.Nazwisko 
                    && search.Postfiks == ulica.Postfiks
                    && search.Prefiks == ulica.Prefiks
                    )
                    // Nie ma imienia ani nazwiska, ale pseudonim się zgadza czyli mjr Hubala
                    return 100;
            }

            if (search.Pseudonim!=""
                && search.Imie == ""
                && search.Imie2 == ""
                && search.Nazwisko == ""
                && search.Nazwisko2 == ""
                && search.Pseudonim == ulica.Nazwisko)
            {
                if (ulica.Pseudonim=="" 
                    && search.Postfiks == ulica.Postfiks
                    && search.Prefiks == ulica.Prefiks
                    )
                    // Szukamy Odrowąża, ktory wszedł jako Pseudonim, ale to w rzeczywistości jest nazwisko
                    return 100;
            }

            // 2. Imię
            if (!string.IsNullOrEmpty(search.Imie))
            {
                totalWeight += IMIE_WEIGHT;
                if (ulica.Imie == search.Imie) { matchedWeight += IMIE_WEIGHT; goto po_imie; }
                // Tu załatwiamy wzorzec: Marii Faustyny Kowalskiej z poszukiwaniem Faustyny Kowalskiej
                if (ulica.Imie2!="" && ulica.Imie2 == search.Imie) { matchedWeight += IMIE_WEIGHT; goto po_imie; }
                 // Tu załatwiamy J. Hallera
                if (SkrotImienia(ulica.Imie,search.Imie)) { matchedWeight += IMIE_WEIGHT; goto po_imie; }
            }
            po_imie:
            // 3. Tytuł
            if (!string.IsNullOrEmpty(search.Tytul))
            {
                totalWeight += TYTUL_WEIGHT;

                if (TitleManager.TenSamTytul(ulica.Tytul, search.Tytul))
                    matchedWeight += TYTUL_WEIGHT;
            }

            // 4. Pseudonim
            if (!string.IsNullOrEmpty(search.Pseudonim))
            {
                totalWeight += PSEUDONIM_WEIGHT;

                if (ulica.Pseudonim == search.Pseudonim)
                    matchedWeight += PSEUDONIM_WEIGHT;
            }

            // 5. Drugie imię
            if (!string.IsNullOrEmpty(search.Imie2))
            {
                totalWeight += IMIE2_WEIGHT;
                if (ulica.Imie2 == search.Imie2) { matchedWeight += IMIE2_WEIGHT; goto po_imie2; }
                if (SkrotImienia(ulica.Imie2, search.Imie2)) { matchedWeight += IMIE2_WEIGHT; goto po_imie2; }
            }
        po_imie2:
            // Oblicz procent dopasowania
            if (totalWeight == 0)
                return 0;

            return (matchedWeight * 100) / totalWeight;
        }

        private bool SkrotImienia(string imie1, string imie2)
        {
            if (imie1.Length < 2) return false;
            if (imie2.Length < 2) return false;
            if (imie1.Length == 2 && imie1[1] == '.' && imie1[0] == imie2[0]) return true;
            if (imie2.Length == 2 && imie2[1] == '.' && imie1[0] == imie2[0]) return true;
            return false;
        }
    }
}
