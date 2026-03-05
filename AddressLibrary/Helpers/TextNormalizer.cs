// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Text.RegularExpressions;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Serwis do normalizacji tekstu (usuwanie akcentów, przedrostków, etc.)
    /// </summary>
    public static class TextNormalizer
    {
        private static readonly string[] titles = new[] { 
                // wojskowe
                "plk","pulkownika",
                "mjr","majora",
                "kpt", "kapitana",
                "por", "porucznika",
                "gen", "generala",
                "pplk", "podpulkownika",
                "rotm", "rtm", "rotmistrza",
                "sierz", "sierzanta",
                "marsz", "marszalka",
                "adm", "admirala",
                "kmdr", "komandora",
                // religijne
                "sw","swietego",
                "ks", "ksiedza","ksiecia",
                "bp", "biskupa",
                "abp", "arcybiskupa",
                "kard", "kardynala",
                "br", "brata",
                "o", "ojca",
                "s", "siostry",
                "bl","blogoslawionego",
                // naukowe
                "dr", "doktora",
                "prof", "profesora",
                "inz", "inzyniera",
                "mgr", "magistra",
                // szlacheckie
                "kr", "krolowej","krola"
            };

        // ✅ NOWE: HashSet z case-insensitive comparer dla szybszego wyszukiwania
        private static readonly HashSet<string> titlesSet = new HashSet<string>(titles, StringComparer.OrdinalIgnoreCase);

        static TextNormalizer()
        {
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.ToLowerInvariant().Trim();
            normalized = UliceUtils.RemoveDiacritics(normalized);
            // Popraw os.Nowe na os. Nowe
            normalized = normalized.Replace("..", ".");
            normalized = normalized.Replace(".", ". ").Trim();
            // usuń ewentualnie powstałe podwójne spacje
            normalized = normalized.Replace("  ", " ").Trim();
            normalized = normalized.Replace("-go", "").Trim();


            if (normalized.Contains("bat."))
            {
            }

            normalized = RemoveTitles(normalized);
            normalized = RemoveInitialsPrefix(normalized);

            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public static string RemoveInitialsPrefix(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Wzorzec: 1-3 litery (polskie lub łacińskie), kropka, ewentualnie powtórzone, na początku napisu
            // Przykłady: "J. ", "A.B. ", "M.K. ", "Ł. ", "J.K. ", "A.B.C. "
            var pattern = @"^(([\p{L}]{1,2}\.)+\s*)+";

            return Regex.Replace(text, pattern, string.Empty).TrimStart();
        }



        /// <summary>
        /// Usuwa tytuły wojskowe, religijne, naukowe z tekstu (case-insensitive, bez polskich znaków)
        /// </summary>
        public static string RemoveTitles(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // ✅ POPRAWKA: Normalizuj każde słowo przed porównaniem (usuń polskie znaki + lowercase)
            var filtered = words.Where(w =>
            {
                var normalized = UliceUtils.RemoveDiacritics(w.Replace(".", "").ToLowerInvariant());
                return !titlesSet.Contains(normalized);
            }).ToList();

            return string.Join(" ", filtered);
        }
    }
}