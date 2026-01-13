// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using System.Globalization;
using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do normalizacji tekstu (usuwanie akcentów, przedrostków, etc.)
    /// </summary>
    public class TextNormalizer
    {
        private static readonly string[] StreetPrefixes = new[]
        {
            "ul.", "ul", "ulica",
            "al.", "al", "aleja", "alei",
            "pl.", "pl", "plac", "placu",
            "os.", "os", "osiedle", "osiedla",
            "oœ.", "oœ",
            "rondo",
            "skwer", "skweru",
            "park", "parku",
            "bulwar", "bulwaru",
            "droga",
            "szosa",
            "œcie¿ka",
            "pasa¿", "pasa¿u"
        };

        /// <summary>
        /// Normalizuje tekst: usuwa akcenty, ma³e litery, usuwa przedrostki ulic
        /// </summary>
        public string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = RemoveDiacritics(text.Trim());
            normalized = normalized.ToLowerInvariant();
            normalized = RemoveStreetPrefixes(normalized);
            // Usuñ numery z nazwy ulicy (np. "Walerego Goetla 12" -> "Walerego Goetla")
            normalized = RemoveTrailingNumbers(normalized, out _);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Normalizuje nazwê ulicy i wyci¹ga z niej numer (jeœli wystêpuje)
        /// </summary>
        public (string NormalizedStreet, string ExtractedNumber) NormalizeStreetWithNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (string.Empty, string.Empty);

            var normalized = RemoveDiacritics(text.Trim());
            normalized = normalized.ToLowerInvariant();
            normalized = RemoveStreetPrefixes(normalized);
            
            // Usuñ numery i zwróæ je
            normalized = RemoveTrailingNumbers(normalized, out string extractedNumber);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return (normalized, extractedNumber);
        }

        /// <summary>
        /// Normalizuje kod pocztowy do formatu XX-XXX
        /// </summary>
        public string NormalizePostalCode(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return string.Empty;
            }

            var cyfry = new string(kod.Where(char.IsDigit).ToArray());

            if (cyfry.Length != 5)
            {
                return kod;
            }

            return $"{cyfry.Substring(0, 2)}-{cyfry.Substring(2, 3)}";
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private string RemoveStreetPrefixes(string text)
        {
            var sortedPrefixes = StreetPrefixes.OrderByDescending(p => p.Length);

            foreach (var prefix in sortedPrefixes)
            {
                // Obs³uga prefiksu ze spacj¹ (np. "ul. Krakowska")
                if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return text.Substring(prefix.Length + 1).Trim();
                }

                // NOWE: Obs³uga prefiksu z kropk¹ bez spacji (np. "ul.Krakowska")
                if (prefix.EndsWith(".") && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var afterPrefix = text.Substring(prefix.Length);
                    // Jeœli po prefiksie jest litera (nie spacja), usuñ prefiks
                    if (afterPrefix.Length > 0 && char.IsLetter(afterPrefix[0]))
                    {
                        return afterPrefix.Trim();
                    }
                }

                // Obs³uga samego prefiksu
                if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
            }

            return text;
        }

        /// <summary>
        /// Usuwa koñcowe numery z nazwy ulicy i zwraca je
        /// </summary>
        private string RemoveTrailingNumbers(string text, out string extractedNumber)
        {
            extractedNumber = string.Empty;

            // ZnajdŸ ostatnie s³owo - jeœli jest liczb¹ lub zaczyna siê od liczby, wyci¹gnij je
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var lastWord = words[^1];
                // SprawdŸ czy ostatnie s³owo to liczba lub zawiera cyfry na pocz¹tku
                if (char.IsDigit(lastWord[0]) || lastWord.All(char.IsDigit))
                {
                    extractedNumber = lastWord;
                    return string.Join(" ", words.Take(words.Length - 1));
                }
            }

            return text;
        }
    }
}