// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Models;
using Microsoft.IdentityModel.Tokens;

namespace AddressLibrary.Services.HierarchyBuilders
{
    /// <summary>
    /// Wynik wyszukiwania ulicy z fuzzy matching
    /// </summary>
    public class UlicaMatchResult
    {
        public Ulica? Ulica { get; set; }
        public int Score { get; set; }
        public string SearchName { get; set; } = string.Empty;
        public bool Found => Ulica != null && Score >= 70;
    }

    /// <summary>
    /// Metody rozszerzeñ dla s³ownika ulic - inteligentne wyszukiwanie z fuzzy matching
    /// </summary>
    public static class UlicaDictionaryExtensions
    {
        /// <summary>
        /// Próbuje znaleŸæ najlepsze dopasowanie ulicy na podstawie podobieñstwa tekstowego
        /// </summary>
        /// <param name="uliceDict">S³ownik ulic w miejscowoœci</param>
        /// <param name="searchName">Szukana nazwa ulicy</param>
        /// <param name="ulica">Znaleziona ulica (jeœli dopasowanie >= 70%)</param>
        /// <returns>True jeœli znaleziono dopasowanie</returns>
        public static bool TryGetValueAgain(this Dictionary<string, Ulica> uliceDict, string searchName, out Ulica? ulica)
        {
            var result = TryGetValueAgainWithScore(uliceDict, searchName);
            ulica = result.Ulica;
            return result.Found;
        }

        /// <summary>
        /// Próbuje znaleŸæ najlepsze dopasowanie ulicy z informacj¹ o wyniku (score)
        /// </summary>
        public static UlicaMatchResult TryGetValueAgainWithScore(this Dictionary<string, Ulica> uliceDict, string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName) || uliceDict.Count == 0)
                return new UlicaMatchResult { SearchName = searchName, Score = 0 };

            int bestScore = 0;
            Ulica? bestMatch = null;

            foreach (var kvp in uliceDict)
            {
                var oUlica = kvp.Value;

                // SprawdŸ Nazwa1
                int score = PoliczNajlepszy(searchName, oUlica.Nazwa1);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = oUlica;
                }

                // SprawdŸ Nazwa2 + Nazwa1 (jeœli Nazwa2 istnieje)
                if (!oUlica.Nazwa2.IsNullOrEmpty())
                {
                    score = PoliczNajlepszy(searchName, oUlica.Nazwa2 + " " + oUlica.Nazwa1);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = oUlica;
                    }
                }
            }

            return new UlicaMatchResult
            {
                Ulica = bestScore >= 70 ? bestMatch : null,
                Score = bestScore,
                SearchName = searchName
            };
        }

        private static int PoliczNajlepszy(string searchName, string ulicaName)
        {
            var searchNameLower = searchName.ToLowerInvariant();
            var searchNameNormalized = NormalizeStreetName(searchNameLower);

            // Normalizuj nazwê ulicy z bazy
            var ulicaNameNormalized = NormalizeStreetName(ulicaName);

            // Oblicz punkty dopasowania
            int score = CalculateSimilarityScore(searchNameNormalized, ulicaNameNormalized);

            return score;
        }

        /// <summary>
        /// Normalizuje nazwê ulicy - usuwa prefiksy, polskie znaki, spacje
        /// </summary>
        private static string NormalizeStreetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Usuñ popularne prefiksy
            var prefixes = new[] { "ul.", "ulica", "al.", "aleja", "alei", "os.", "osiedle", "pl.", "plac", "placu" };
            var normalized = name.ToLowerInvariant().Trim();

            foreach (var prefix in prefixes)
            {
                if (normalized.StartsWith(prefix + " "))
                    normalized = normalized.Substring(prefix.Length + 1).Trim();
                else if (normalized.StartsWith(prefix))
                    normalized = normalized.Substring(prefix.Length).Trim();
            }

            // Zamieñ polskie znaki na ich odpowiedniki ³aciñskie
            normalized = normalized
                .Replace('¹', 'a').Replace('æ', 'c').Replace('ê', 'e')
                .Replace('³', 'l').Replace('ñ', 'n').Replace('ó', 'o')
                .Replace('œ', 's').Replace('Ÿ', 'z').Replace('¿', 'z');

            // Usuñ znaki specjalne i wielokrotne spacje
            normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray());
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Oblicza punkty podobieñstwa miêdzy dwoma tekstami (0-100)
        /// Wykorzystuje odleg³oœæ Levenshteina i analizê wspólnych prefiksów
        /// </summary>
        private static int CalculateSimilarityScore(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0;

            // Dok³adne dopasowanie = 100 punktów
            if (text1 == text2)
                return 100;

            // Jeden tekst zawiera drugi = 90 punktów
            if (text1.Contains(text2) || text2.Contains(text1))
                return 90;

            // Odleg³oœæ Levenshteina
            int distance = LevenshteinDistance(text1, text2);
            int maxLength = Math.Max(text1.Length, text2.Length);

            // Przelicz na procent podobieñstwa
            int similarity = (int)(100.0 * (1.0 - (double)distance / maxLength));

            // Bonus za te same pocz¹tkowe znaki (maksymalnie +10 punktów)
            int commonPrefixLength = GetCommonPrefixLength(text1, text2);
            if (commonPrefixLength > 3)
                similarity += Math.Min(10, commonPrefixLength - 3);

            return Math.Min(100, similarity);
        }

        /// <summary>
        /// Oblicza odleg³oœæ Levenshteina miêdzy dwoma tekstami
        /// (minimalna liczba operacji edycji potrzebnych do przekszta³cenia jednego tekstu w drugi)
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            // Inicjalizacja pierwszego wiersza i kolumny
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            // Wype³nij macierz
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1,      // usuniêcie
                                 d[i, j - 1] + 1),      // wstawienie
                        d[i - 1, j - 1] + cost);        // zamiana
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Zwraca d³ugoœæ wspólnego prefiksu dwóch tekstów
        /// </summary>
        private static int GetCommonPrefixLength(string s1, string s2)
        {
            int minLength = Math.Min(s1.Length, s2.Length);
            int count = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (s1[i] == s2[i])
                    count++;
                else
                    break;
            }

            return count;
        }
    }
}