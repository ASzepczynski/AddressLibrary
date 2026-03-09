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
            normalized = RemoveInitialsPrefix(normalized);
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
    }
}