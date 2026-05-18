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

        /// <summary>
        /// Najczęstsze cechy ulic — kandydaci preferowani gdy cecha wyszukiwanej ulicy nie pasuje
        /// </summary>
        private static readonly List<string> NajczestszeCechy =
        [
            "ulica", "aleja", "osiedle", "aleje"
        ];

        public StreetMatcher(StreetParser parser)
        {
            _parser = parser;
        }


        /// <summary>
        /// 🚀 Strukturalne dopasowywanie komponentów ulicy
        /// Znajduje ulicę w liście UlicaCached 
        /// 
        /// </summary>
        public List<UlicaCached>? FindStreet(List<UlicaCached> ulice, string streetName, string originalName, string dzielnica, out bool wasFuzzy, out string info)
        {
            var wzorek = "serkowskiego";
            wasFuzzy = false;
            info = "";
            if (string.IsNullOrWhiteSpace(streetName))
                return null;
            (string sCecha, string normalizedSearch) = CechyUlicUtils.SplitStreetPrefix(streetName);

            normalizedSearch = TextNormalizer.Normalize(normalizedSearch);

            var parsed = _parser.Parse(normalizedSearch);

            var nowaUlica = new UlicaCached
            {
                Prefiks = parsed.Prefiks,
                Tytul = parsed.Tytul,
                Imie = parsed.Imie,
                Imie2 = parsed.Imie2,
                Nazwisko = parsed.Nazwisko,
                Nazwisko2 = parsed.Nazwisko2,
                Pseudonim = parsed.Pseudonim,
                Postfiks = parsed.Postfiks,
            };

            var parsedSearch = TextNormalizer.Normalize(nowaUlica.GetShortName());

            var listaPotencjalne = new List<(UlicaCached Ulica, int Score)>();

            // Najpierw sprawdzamy wprost - po nazwie
            foreach (var ulica in ulice)
            {

                var normalizedShort = ulica.NormalizedShortName;
                var normalizedFull = ulica.NormalizedFullName;

                if (normalizedShort.Contains(wzorek))
                {
                    int y = 1;
                }

                if (normalizedShort == normalizedSearch
                 || normalizedShort == parsedSearch
                 || normalizedFull == normalizedSearch
                 || normalizedFull == parsedSearch
                    )
                {
                    if (!listaPotencjalne.Any(x => x.Ulica.Id == ulica.Id))
                        listaPotencjalne.Add(new(ulica, 0));
                    continue;
                }
            }

            var kandydaci = ZweryfikujKandydatow(listaPotencjalne, sCecha,originalName,dzielnica);
            if (kandydaci.Count() == 1)
            {
                return kandydaci.Select(x=>x.Ulica).ToList();
            }
            else
            {
                if (kandydaci.Count() > 1)
                {
                    info = "Więcej niż 1 nazwa ulicy pasuje, ale cecha lub dzielnica się nie zgadza";
                    return kandydaci.Select(x=>x.Ulica).ToList();
                }
            }
            // Teraz z wyceną, bo powyższe wyszukiwanie zwróciło zero
            UlicaCached? bestMatch = null;
            int bestScore = 0;
            foreach (var ulica in ulice)
            {

                var normalizedShort = ulica.NormalizedShortName;
                if (normalizedShort.Contains(wzorek))
                {
                    int y = 1;
                }
                var score = CalculateMatchScore(parsed, ulica);

                if (score >= 70 && !listaPotencjalne.Any(x => x.Ulica.Id == ulica.Id))
                    listaPotencjalne.Add(new(ulica, score));

            }

            kandydaci = ZweryfikujKandydatow(listaPotencjalne, sCecha,originalName,dzielnica);
            if (kandydaci.Count() == 1)
            {
                // Jeśli dobrze pasuje tylko jedna ulica to ją zwróć
                wasFuzzy = kandydaci[0].Score != 100;
                return kandydaci.Select(x => x.Ulica).ToList();
            }
            if (kandydaci.Count() > 1)
            {
                info = "Więcej niż 1 nazwa ulicy pasuje, nie mogę zdecydować";
                return kandydaci.Select(x => x.Ulica).ToList();
            }

            // Brak kandydatów
            // Wszystko przepadło — spróbujmy znaleźć najlepsze dopasowanie używając odległości Levenshteina
            wasFuzzy = true;

            listaPotencjalne.Clear();
            
            foreach (var ulica in ulice)
            {
                var candidateShort = ulica.NormalizedShortName ?? string.Empty;
                var candidateFull = ulica.NormalizedFullName ?? string.Empty;
                var onlyLastname = TextNormalizer.Normalize(ulica.Nazwisko);

                if (candidateFull.Contains(wzorek))
                {
                    int y = 1;
                }

                int pShort = GetLevenshteinPercent(parsedSearch, candidateShort);
                int pFull = GetLevenshteinPercent(parsedSearch, candidateFull);
                int pNazwisko = 0;
                if(!parsedSearch.Contains(" ") && ulica.Nazwisko!="")
                {
                    // Dla ulic jednosłowowowych
                    pNazwisko = GetLevenshteinPercent(parsedSearch, onlyLastname);

                }

                int score = Math.Max(Math.Max(pShort, pFull),pNazwisko);

                if (score < 80) continue;
                listaPotencjalne.Add(new (ulica, score));
            }

            if (listaPotencjalne.Count == 0) return null;

            // Ustawiamy listaPotencjalne na najlepsze dopasowania znalezione przez Levenshteina

            kandydaci = ZweryfikujKandydatow(listaPotencjalne, sCecha,originalName,dzielnica);

            if (kandydaci.Count() == 1)
            {
                // Jeśli dobrze pasuje tylko jedna ulica to ją zwróć
                return kandydaci.Select(x => x.Ulica).ToList();
            }
            if (kandydaci.Count() > 1)
            {
                info = "Więcej niż 1 nazwa ulicy pasuje, niejednonzaczność";
                return kandydaci.Select(x => x.Ulica).ToList();
            }

            return null;
        }

// Metoda próbuje rozstrzygnąć o jaką ulicę chodziło w przypadku niejednoznaczności

        public List<(UlicaCached Ulica, int Score)> ZweryfikujKandydatow(List<(UlicaCached Ulica, int Score)> listaPotencjalne, string sCecha, string originalName, string dzielnica)
        {

            if (listaPotencjalne.Count() == 1)
            {
                return listaPotencjalne;
            }

            List<(UlicaCached Ulica, int Score)> wynik;
            // Szukamy ulicy z właściwą cechą i dzielnicą
            var kandydaci = listaPotencjalne
                 .Where(x => x.Ulica.Dzielnica==dzielnica && 
                       (x.Ulica.CechaUlicy.Skrot == sCecha
                    || x.Ulica.CechaUlicy.Nazwa == sCecha)
            );
            if (PasujeJeden(kandydaci, out wynik))return wynik;

            // Szukamy ulicy z właściwą dzielnicą
            kandydaci = listaPotencjalne
                 .Where(x => x.Ulica.Dzielnica == dzielnica);
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            // Szukamy ulicy z właściwą cechą
            kandydaci = listaPotencjalne
                 .Where(x => x.Ulica.CechaUlicy.Skrot == sCecha
                    || x.Ulica.CechaUlicy.Nazwa == sCecha);
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            // Szukamy ulicy z prawidłową nazwą Sądowa/Sadowa Łąkowa/Lakowa
            kandydaci = listaPotencjalne
                 .Where(x => x.Ulica.OriginalName == originalName);
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            // Szukamy ulicy z prawidłową nazwą Sądowa/Sadowa Łąkowa/Lakowa i pasującą dzielnicą
            kandydaci = listaPotencjalne
                 .Where(x => x.Ulica.OriginalName == originalName && x.Ulica.Dzielnica == dzielnica);
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            // Szukamy trafienia 100%
            kandydaci = listaPotencjalne.Where(x => x.Score==100);
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            // Szukamy ulicy z najczęściej używaną cechą
            kandydaci = listaPotencjalne.Where(x => NajczestszeCechy.Contains(x.Ulica.CechaUlicy.Nazwa, StringComparer.OrdinalIgnoreCase));
            if (PasujeJeden(kandydaci, out wynik)) return wynik;

            return kandydaci.OrderByDescending(x => x.Score).ToList();
        }

        public bool PasujeJeden(IEnumerable<(UlicaCached Ulica, int Score)> kandydaci, out List<(UlicaCached Ulica, int Score)> wynik)
        {
            wynik = kandydaci.Where(x=>x.Score!=2*x.Score).ToList(); // specjalnie żeby lista była pusta
            if (kandydaci.Count() == 1)
            {
                wynik = kandydaci.ToList();
                return true;
            }
            return false;
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

            if (ulica.Prefiks != search.Prefiks) return 0;
            if (ulica.Postfiks != search.Postfiks) return 0;
//            if (ulica.Nazwisko != search.Nazwisko) return 0;

            // 0. Dla królowej Jadwigi

            if (search.Nazwisko == ""
                && ulica.Nazwisko == ""
                && search.Nazwisko2 == ""
                && ulica.Nazwisko2 == ""
                && search.Imie != ""
                && ulica.Postfiks == search.Postfiks
                && ulica.Prefiks == search.Prefiks
                && ulica.Pseudonim == search.Pseudonim
                )
            {
                int score = 0;
                if (ulica.Imie == search.Imie && ulica.Imie2 == search.Imie2)
                {
                    score = 80;
                }
                if (TitleManager.TenSamTytul(ulica.Tytul, search.Tytul))
                {
                    score += TYTUL_WEIGHT;
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

            if (!string.IsNullOrEmpty(search.Pseudonim) && search.Pseudonim == ulica.Pseudonim)
            {
                if ( (search.Imie == ulica.Imie || search.Imie=="")
                    && (search.Nazwisko == ulica.Nazwisko || search.Nazwisko=="")
                    && search.Postfiks == ulica.Postfiks
                    && search.Prefiks == ulica.Prefiks
                    )
                    // Nie ma imienia ani nazwiska, ale pseudonim się zgadza czyli mjr Hubala
                    return 100;
            }

            if (search.Pseudonim != ""
                && search.Imie == ""
                && search.Imie2 == ""
                && search.Nazwisko == ""
                && search.Nazwisko2 == ""
                && search.Pseudonim == ulica.Nazwisko)
            {
                if (ulica.Pseudonim == ""
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
                if (ulica.Imie2 != "" && ulica.Imie2 == search.Imie) { matchedWeight += IMIE_WEIGHT; goto po_imie; }
                // Tu załatwiamy J. Hallera
                if (SkrotImienia(ulica.Imie, search.Imie)) { matchedWeight += IMIE_WEIGHT; goto po_imie; }
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

        // Oblicza odległość Levenshteina i zwraca procent podobieństwa (0-100)
        private static int GetLevenshteinPercent(string a, string b)
        {
            if (a == null) a = string.Empty;
            if (b == null) b = string.Empty;
            if (a.Length == 0 && b.Length == 0) return 100;
            int dist = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(a,b);
            int max = Math.Max(a.Length, b.Length);
            if (max == 0) return 100;
            double pct = 100.0 * (max - dist) / max;
            if (pct < 0) pct = 0;
            return (int)Math.Round(pct);
        }

      
    }
}
