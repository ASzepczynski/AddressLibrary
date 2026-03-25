// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

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
        /// Sprawdza czy ulica pasuje do wyszukiwanej nazwy (dokładne dopasowanie = 100% score)
        /// </summary>
        public bool IsMatch(UlicaCached ulica, string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return false;

            // ✅ KROK 1: Jeśli ulica jest nie-osobowa (brak komponentów) - dopasuj po pełnej nazwie
            if (ulica.IsEmpty())
            {
                var normalizedSearch = TextNormalizer.Normalize(streetName);
                var normalizedFull = ulica.GetFullNormalized();
                return normalizedFull == normalizedSearch;
            }

            // ✅ KROK 2: Jeśli ulica jest osobowa - parsuj i dopasuj komponenty
            var parsed = _parser.Parse(streetName);

            // Dokładne dopasowanie wymaga 100% score
            var score = CalculateMatchScore(parsed, ulica);

            return score == 100;
        }

        /// <summary>
        /// 🚀 Strukturalne dopasowywanie komponentów ulicy
        /// Znajduje ulicę w liście UlicaCached na podstawie nazwy (fuzzy matching)
        /// </summary>
        public UlicaCached? FindStreet(List<UlicaCached> ulice, string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return null;

            var normalizedSearch = TextNormalizer.Normalize(streetName);

            // ✅ KROK 1: Znajdź ulice nie-osobowe (proste nazwy)
            foreach (var ulica in ulice)
            {
                if (ulica.IsEmpty())
                {
                    var normalizedFull = ulica.GetFullNormalized();

                    // Dokładne dopasowanie
                    if (normalizedFull == normalizedSearch)
                        return ulica;
                }
            }

            // ✅ KROK 2: Parsuj nazwę dla ulic osobowych
            var parsed = _parser.Parse(streetName);

            // ✅ KROK 3: Dopasuj do ulic osobowych
            UlicaCached? bestMatch = null;
            int bestScore = 0;

            foreach (var ulica in ulice)
            {
                // Pomiń ulice nie-osobowe (już sprawdzone w KROK 1)
                if (ulica.IsEmpty())
                    continue;

                // Sprawdź dopasowanie cechy
                if (!string.IsNullOrEmpty(parsed.Cecha) &&
                    !string.IsNullOrEmpty(ulica.Cecha) &&
                    TextNormalizer.Normalize(ulica.Cecha) != parsed.Cecha)
                {
                    continue; // Cecha się nie zgadza - pomiń
                }

                // Oblicz score dopasowania komponentów
                var score = CalculateMatchScore(parsed, ulica);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = ulica;
                }
            }

            // ✅ KROK 4: Fuzzy matching dla ulic nie-osobowych (jeśli nie znaleziono osobowej)
            if (bestMatch == null || bestScore < 70)
            {
                foreach (var ulica in ulice)
                {
                    if (ulica.IsEmpty())
                    {
                        var normalizedFull = ulica.GetFullNormalized();

                        // Częściowe dopasowanie (contains)
                        if (normalizedFull.Contains(normalizedSearch) || normalizedSearch.Contains(normalizedFull))
                        {
                            int distance = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(normalizedSearch, normalizedFull);
                            if (distance <= 2)
                            {
                                return ulica;
                            }
                        }
                    }
                }
            }

            // Wymagamy minimum 70% dopasowania dla ulic osobowych
            return bestScore >= 70 ? bestMatch : null;
        }

        /// <summary>
        /// 🔍 DIAGNOSTYKA: Znajduje wszystkie ulice z ich score'ami (do debugowania)
        /// </summary>
        public List<(UlicaCached ulica, int score, string reason, ParsedStreet? parsed)> FindAllWithScores(List<UlicaCached> ulice, string streetName)
        {
            var results = new List<(UlicaCached ulica, int score, string reason, ParsedStreet? parsed)>();

            if (string.IsNullOrWhiteSpace(streetName))
                return results;

            var normalizedSearch = TextNormalizer.Normalize(streetName);

            Oddrukuj(ulice);

            // KROK 1: Ulice nie-osobowe (proste nazwy)
            foreach (var ulica in ulice)
            {
                if (ulica.IsEmpty())
                {
                    var normalizedFull = ulica.GetFullNormalized();

                    if (normalizedFull == normalizedSearch)
                    {
                        results.Add((ulica, 100, "Dokładne dopasowanie nie-osobowej", null));
                    }
                    else if (normalizedFull.Contains(normalizedSearch) || normalizedSearch.Contains(normalizedFull))
                    {
                        int distance = AddressLibrary.Utils.Levenshtein.CalculateLevenshteinDistance(normalizedSearch, normalizedFull);
                        if (distance <= 2)
                        {
                            results.Add((ulica, 50 - (distance * 10), $"Fuzzy nie-osobowej (dist={distance})", null));
                        }
                    }
                }
            }

            // KROK 2: Ulice osobowe - parsuj i oblicz score
            var parsed = _parser.Parse(streetName);



            foreach (var ulica in ulice)
            {



                if (ulica.IsEmpty())
                    continue; // Już sprawdzone w KROK 1



                // Sprawdź cechę
                if (!string.IsNullOrEmpty(parsed.Cecha) &&
                    !string.IsNullOrEmpty(ulica.Cecha) &&
                    TextNormalizer.Normalize(ulica.Cecha) != parsed.Cecha)
                {
                    results.Add((ulica, 0, "Cecha się nie zgadza", parsed));
                    continue;
                }

                var score = CalculateMatchScore(parsed, ulica);

                string reason = score switch
                {
                    100 => "Dokładne dopasowanie komponentów",
                    >= 70 => $"Częściowe dopasowanie komponentów (score={score})",
                    > 0 => $"Słabe dopasowanie komponentów (score={score})",
                    _ => "Brak dopasowania komponentów (score=0)"
                };

                results.Add((ulica, score, reason, parsed));
            }

            return results.OrderByDescending(r => r.score).ToList();
        }

        /// <summary>
        /// Oblicza score dopasowania (0-100) porównując komponenty
        /// ⚠️ UWAGA: Jeśli ulica nie ma nazwiska, zwraca 0 (nie jest osobowa)
        /// </summary>
        private int CalculateMatchScore(ParsedStreet search, UlicaCached ulica)
        {
            int totalWeight = 0;
            int matchedWeight = 0;

            // WAGI komponentów
            const int NAZWISKO_WEIGHT = 50;
            const int IMIE_WEIGHT = 20;
            const int TYTUL_WEIGHT = 15;
            const int PSEUDONIM_WEIGHT = 10;
            const int IMIE2_WEIGHT = 5;


            // 0. Dla królowej Jadwigi


            if (string.IsNullOrEmpty(search.Nazwisko) && !string.IsNullOrEmpty(search.Imie))
            {
                if (ulica.Imie == "kingi" && search.Imie=="kingi")
                {
                    int y = 1;
                }
                int score = 0;

                if (ulica.Imie == search.Imie)
                {
                    if (ulica.Postfiks == search.Postfiks && ulica.Prefiks == search.Prefiks)
                    {
                        score = 80;
                    }
                    if (TitleManager.TenSamTytul(ulica.Tytul, search.Tytul))
                    {
                        score += 20;
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
                    matchedWeight += NAZWISKO_WEIGHT / 2;
                }
                else
                {
                    // Nazwisko nie pasuje - zwróć 0
                    return 0;
                }
            }
            else
            {
                // ⚠️ Brak nazwiska w search - to może być ulica nie-osobowa
                // Zwróć 0, aby wymusić dopasowanie przez pełną nazwę
                return 0;
            }

            // 2. Imię
            if (!string.IsNullOrEmpty(search.Imie))
            {
                totalWeight += IMIE_WEIGHT;

                if (ulica.Imie == search.Imie)
                    matchedWeight += IMIE_WEIGHT;
                else if (ulica.Imie2 == search.Imie)
                    matchedWeight += IMIE_WEIGHT / 2;
            }

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

                if (ulica.Imie2 == search.Imie2)
                    matchedWeight += IMIE2_WEIGHT;
            }

            // Oblicz procent dopasowania
            if (totalWeight == 0)
                return 0;

            return (matchedWeight * 100) / totalWeight;
        }

        private void Oddrukuj(List<UlicaCached> ulice)
        {

            return;

            try
            {
                var debugLines = new System.Text.StringBuilder();
                debugLines.AppendLine($"Liczba ulic: {ulice.Count}");
                debugLines.AppendLine(new string('-', 100));
                debugLines.AppendLine($"" +
                $"ID|" +
                $"Cecha|" +
                $"Prefiks|" +
                $"Tytuł|" +
                $"Imie|" +
                $"Imie2|" +
                $"Nazwisko|" +
                $"Nazwisko2|" +
                $"Pseudonim|" +
                $"Postfiks|" +
                $"IsEmpty");

                foreach (var u in ulice)
                {
                    debugLines.AppendLine($"" +
                        $"{u.Id}|" +
                        $"{u.Cecha}|" +
                        $"{u.Prefiks}|" +
                        $"{u.Tytul}|" +
                        $"{u.Imie}|" +
                        $"{u.Imie2}|" +
                        $"{u.Nazwisko}|" +
                        $"{u.Nazwisko2}|" +
                        $"{u.Pseudonim}|" +
                        $"{u.Postfiks}|" +
                        $"{u.IsEmpty()}");
                }

                debugLines.AppendLine(new string('=', 100));
                System.IO.File.WriteAllText(@"C:\dane\UliceMiasta.txt", debugLines.ToString(), System.Text.Encoding.UTF8);
            }
            catch { /* Ignoruj błędy zapisu */ }
        }
    }
}
