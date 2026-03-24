// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Text.RegularExpressions;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Serwis do normalizacji tekstu (usuwanie akcentów, przedrostków, etc.)
    /// </summary>
    public static class TextNormalizer
    {

        public static string MakeCorrections(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Replace("..", ".");
            normalized = normalized.Replace(".", ". ").Trim();
            // usuń ewentualnie powstałe podwójne spacje
            normalized = normalized.Replace("  ", " ").Trim();
            normalized = normalized.Replace("-go", "").Trim();
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var normalized = MakeCorrections(text);
            normalized = normalized.ToLowerInvariant().Trim();
            normalized = UliceUtils.RemoveDiacritics(normalized);
            normalized = TitleManager.RemoveTitles(normalized);
            normalized = RemoveNamePrefixes(normalized);
            normalized = RemoveInitialsPrefix(normalized);
            return normalized;
        }

        /// <summary>
        /// Usuwa prefiksy związane z patronami ulic: "im.", "imienia", "imieniem" (case insensitive)
        /// Przykłady: "im. Kowalskiego" -> "Kowalskiego", "Imienia Jana Pawła" -> "Jana Pawła"
        /// </summary>
        public static string RemoveNamePrefixes(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Wzorzec: na początku tekstu znajduje się "im.", "imienia" lub "imieniem" (case insensitive) + spacja
            // Flaga RegexOptions.IgnoreCase zapewnia case insensitive
            var pattern = @"^(im\.|imienia|imieniem)\s+";

            return Regex.Replace(text, pattern, string.Empty, RegexOptions.IgnoreCase).TrimStart();
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
    }
}