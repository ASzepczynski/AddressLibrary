// Copyright (c) 2025-2026 Andrzej Szepczynski. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.HierarchyBuilders
{
    /// <summary>
    /// Wynik wyszukiwania miejscowoœci z fuzzy matching
    /// </summary>
    public class MiejscowoscMatchResult
    {
        public Miejscowosc? Miejscowosc { get; set; }
        public int Score { get; set; }
        public string SearchName { get; set; } = string.Empty;
        public bool Found => Miejscowosc != null && Score >= 70;
    }

    /// <summary>
    /// Metody rozszerzeñ dla s³ownika miejscowoœci - inteligentne wyszukiwanie z fuzzy matching
    /// </summary>
    public static class MiejscowoscDictionaryExtensions
    {
        /// <summary>
        /// Próbuje znaleŸæ najlepsze dopasowanie miejscowoœci na podstawie podobieñstwa tekstowego
        /// </summary>
        /// <param name="miejscowosciDict">S³ownik miejscowoœci w gminie</param>
        /// <param name="searchName">Szukana nazwa miejscowoœci</param>
        /// <param name="miejscowosc">Znaleziona miejscowoœæ (jeœli dopasowanie >= 70%)</param>
        /// <returns>True jeœli znaleziono dopasowanie</returns>
        public static bool TryGetValueAgain(this Dictionary<string, Miejscowosc> miejscowosciDict, string searchName, out Miejscowosc? miejscowosc)
        {
            var result = TryGetValueAgainWithScore(miejscowosciDict, searchName);
            miejscowosc = result.Miejscowosc;
            return result.Found;
        }

        /// <summary>
        /// Próbuje znaleŸæ najlepsze dopasowanie miejscowoœci z informacj¹ o wyniku (score)
        /// </summary>
        public static MiejscowoscMatchResult TryGetValueAgainWithScore(this Dictionary<string, Miejscowosc> miejscowosciDict, string searchName)
        {
            if (string.IsNullOrWhiteSpace(searchName) || miejscowosciDict.Count == 0)
                return new MiejscowoscMatchResult { SearchName = searchName, Score = 0 };

            int bestScore = 0;
            Miejscowosc? bestMatch = null;

            foreach (var kvp in miejscowosciDict)
            {
                var oMiejscowosc = kvp.Value;

                // SprawdŸ nazwê miejscowoœci
                int score = PoliczPodobienstwo(searchName, oMiejscowosc.Nazwa);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = oMiejscowosc;
                }
            }

            return new MiejscowoscMatchResult
            {
                Miejscowosc = bestScore >= 70 ? bestMatch : null,
                Score = bestScore,
                SearchName = searchName
            };
        }

        private static int PoliczPodobienstwo(string searchName, string miejscowoscName)
        {
            var searchNameNormalized = NormalizeName(searchName);
            var miejscowoscNameNormalized = NormalizeName(miejscowoscName);

            // Oblicz punkty dopasowania
            int score = CalculateSimilarityScore(searchNameNormalized, miejscowoscNameNormalized);

            return score;
        }

        /// <summary>
        /// Normalizuje nazwê miejscowoœci - usuwa polskie znaki, spacje dodatkowe
        /// </summary>
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var normalized = name.ToLowerInvariant().Trim();

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
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1,
                                 d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
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