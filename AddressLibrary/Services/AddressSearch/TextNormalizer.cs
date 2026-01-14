// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
            "oś.", "oś",
            "rondo",
            "skwer", "skweru",
            "park", "parku",
            "bulwar", "bulwaru",
            "droga",
            "szosa",
            "ścieżka",
            "pasaż", "pasażu"
        };

        // 🚀 OPTYMALIZACJA: Skompilowane regex (tylko raz!)
        private static readonly Regex TitleAbbreviationsRegex;
        private static readonly Regex RemoveDotsRegex;

        static TextNormalizer()
        {
            // Lista skrótów bez kropek dla wzorca
            var abbrs = new[] { "sw", "ks", "gen", "bp", "kpt", "pplk", "plk", "mjr", "por",
                               "dr", "prof", "inz", "mgr", "abp", "kard", "o", "s", "br",
                               "rotm", "rtm", "sierz", "marsz", "adm", "kmdr", "kr", "bl" };

            // Wzorzec: (sw|ks|gen|...)\.([^\s]) - dodaj spację po kropce
            var pattern1 = $@"({string.Join("|", abbrs)})\.([^\s])";
            TitleAbbreviationsRegex = new Regex(pattern1, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Wzorzec: (sw|ks|gen|...)\. - usuń kropkę
            var pattern2 = $@"({string.Join("|", abbrs)})\.";
            RemoveDotsRegex = new Regex(pattern2, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Normalizuje tekst: usuwa akcenty, małe litery, usuwa przedrostki ulic
        /// </summary>
        public string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = RemoveDiacritics(text.Trim());
            normalized = normalized.ToLowerInvariant();
            
            // 🚀 OPTYMALIZACJA: Jeden regex zamiast pętli
            normalized = TitleAbbreviationsRegex.Replace(normalized, "$1. $2"); // Dodaj spację
            normalized = RemoveDotsRegex.Replace(normalized, "$1"); // Usuń kropkę
            normalized = normalized.Replace('-', ' '); // Zamień myślniki
            
            normalized = RemoveStreetPrefixes(normalized);
            normalized = RemoveTrailingNumbers(normalized, out _);
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Normalizuje nazwę ulicy i wyciąga z niej numer (jeśli występuje)
        /// </summary>
        public (string NormalizedStreet, string ExtractedNumber) NormalizeStreetWithNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (string.Empty, string.Empty);

            var normalized = RemoveDiacritics(text.Trim());
            normalized = normalized.ToLowerInvariant();
            
            // 🚀 OPTYMALIZACJA: Jeden regex zamiast pętli
            normalized = TitleAbbreviationsRegex.Replace(normalized, "$1. $2");
            normalized = RemoveDotsRegex.Replace(normalized, "$1");
            normalized = normalized.Replace('-', ' ');
            
            normalized = RemoveStreetPrefixes(normalized);
            normalized = RemoveTrailingNumbers(normalized, out string extractedNumber);
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return (normalized, extractedNumber);
        }

        /// <summary>
        /// 🆕 Usuwa skrót imienia z początku nazwy ulicy (G.Zapolskiej -> Zapolskiej)
        /// </summary>
        public string RemoveNameInitial(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Wzorzec: jedna wielka litera, kropka, opcjonalnie spacja, następnie reszta nazwy
            // Przykłady: G.Zapolskiej, J.Lea, E.Kwiatkowskiego, J. Pawła II
            var match = Regex.Match(text, @"^[A-ZĄĆĘŁŃÓŚŹŻ]\.\s?(.+)$");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return text;
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
            var stringBuilder = new StringBuilder(text.Length);

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
            // 🚀 OPTYMALIZACJA: Sprawdź najdłuższe prefiksy najpierw
            foreach (var prefix in StreetPrefixes)
            {
                if (text.StartsWith(prefix + " "))
                    return text.Substring(prefix.Length + 1).Trim();

                if (prefix.EndsWith(".") && text.StartsWith(prefix))
                {
                    var afterPrefix = text.Substring(prefix.Length);
                    if (afterPrefix.Length > 0 && char.IsLetter(afterPrefix[0]))
                        return afterPrefix.Trim();
                }

                if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;
            }

            return text;
        }

        /// <summary>
        /// Usuwa końcowe numery z nazwy ulicy i zwraca je
        /// </summary>
        private string RemoveTrailingNumbers(string text, out string extractedNumber)
        {
            extractedNumber = string.Empty;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var lastWord = words[^1];
                // Sprawdź czy ostatnie słowo to liczba lub zawiera cyfry na początku
                if (lastWord.Length > 0 && char.IsDigit(lastWord[0]))
                {
                    extractedNumber = lastWord;
                    return string.Join(" ", words.AsSpan(0, words.Length - 1).ToArray());
                }
            }

            return text;
        }
    }
}