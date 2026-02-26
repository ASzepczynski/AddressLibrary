// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using System.Text.RegularExpressions;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do normalizacji tekstu (usuwanie akcentów, przedrostków, etc.)
    /// </summary>
    public class TextNormalizer
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



        // ✅ NOWE: Skróty nazw miast które NIE MOGĄ BYĆ USUWANE!
        private static readonly string[] CityAbbreviations = new[]
        {
            "św.", "św", "sw.", "sw",     // Święty/Świętokrzyski
            "wlk.", "wlk",                 // Wielki/Wielka
            "maz.", "maz",                 // Mazowiecki
            "śl.", "śl", "sl.", "sl",     // Śląski
            "podh.", "podh",               // Podhalański
            "górn.", "górn", "gorn.", "gorn", // Górny
            "doln.", "doln"                // Dolny
        };



        static TextNormalizer()
        {
        }

        public string Normalize(string text)
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

            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public string RemoveInitialsPrefix(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Wzorzec: 1-3 litery (polskie lub łacińskie), kropka, ewentualnie powtórzone, na początku napisu
            // Przykłady: "J. ", "A.B. ", "M.K. ", "Ł. ", "J.K. ", "A.B.C. "
            var pattern = @"^(([\p{L}]{1,2}\.)+\s*)+";

            return Regex.Replace(text, pattern, string.Empty).TrimStart();
        }



        /// <summary>
        /// Usuwa tytuły wojskowe, religijne, naukowe z tekstu
        /// </summary>
        private string RemoveTitles(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var filtered = words.Where(w => !titles.Contains(w.Replace(".", ""))).ToList();

            return string.Join(" ", filtered);
        }

    }
}