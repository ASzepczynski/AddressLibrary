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
            normalized = normalized.Replace("  ", " ").Trim();

            // Zastąpienie Regex.Replace(normalized, @"\s+", " ") — scalenie białych znaków
            normalized = CollapseWhitespace(normalized);

            // Zastąpienie Regex.Replace(normalized, @"(\d)\.", "$1") — usunięcie kropki po cyfrze
            normalized = RemoveDotAfterDigit(normalized);

            return normalized;
        }

        private static string CollapseWhitespace(string text)
        {
            var buf = new char[text.Length];
            int len = 0;
            bool prevWasSpace = false;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!prevWasSpace && len > 0)
                    {
                        buf[len++] = ' ';
                    }
                    prevWasSpace = true;
                }
                else
                {
                    buf[len++] = c;
                    prevWasSpace = false;
                }
            }
            // usuń końcową spację jeśli istnieje
            if (len > 0 && buf[len - 1] == ' ')
                len--;
            return new string(buf, 0, len);
        }

        private static string RemoveDotAfterDigit(string text)
        {
            // jeśli nie ma żadnej cyfry, wróć od razu
            bool hasDot = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '.' && i > 0 && char.IsDigit(text[i - 1]))
                {
                    hasDot = true;
                    break;
                }
            }
            if (!hasDot)
                return text;

            var buf = new char[text.Length];
            int len = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '.' && i > 0 && char.IsDigit(text[i - 1]))
                    continue; // pomiń kropkę po cyfrze
                buf[len++] = text[i];
            }
            return new string(buf, 0, len);
        }

        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var normalized = MakeCorrections(text);
            normalized = normalized.ToLowerInvariant().Trim();
            normalized = UliceUtils.RemoveDiacritics(normalized);
//            normalized = TitleManager.RemoveTitles(normalized);
//            normalized = RemoveNamePrefixes(normalized);
//            normalized = RemoveInitialsPrefix(normalized);
            return normalized;
        }
      
    }
}