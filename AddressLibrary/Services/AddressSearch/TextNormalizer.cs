// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AddressLibrary.Helpers;
using Microsoft.IdentityModel.Tokens;

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

        // 🚀 OPTYMALIZACJA: Skompilowane regex (tylko raz!)
        private static readonly Regex TitleAbbreviationsRegex;
        private static readonly Regex RemoveDotsRegex;
        private static readonly Regex RemoveGoSuffixRegex;

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

            // Wzorzec: (\d+)-go\s+ - usuń "-go " z dat (np. "3-go Maja" -> "3 Maja")
            RemoveGoSuffixRegex = new Regex(@"(\d+)-go\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = RemoveDiacritics(text.Trim());
            normalized = normalized.ToLowerInvariant();
            normalized = RemoveStreetPrefixes(normalized); // ← TU JEST WYWOŁYWANE
            normalized = RemoveTrailingNumbers(normalized, out _);
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        private string RemoveStreetPrefixes(string text)
        {
            // ✅ WALIDACJA: Sprawdź czy ostatnie słowo to skrót nazwy miasta
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var lastWord = words[^1];
                
                // Sprawdź czy to skrót miasta (z kropką lub bez)
                foreach (var cityAbbr in CityAbbreviations)
                {
                    if (lastWord.Equals(cityAbbr, StringComparison.OrdinalIgnoreCase))
                    {
                        // To jest skrót miasta - ZATRZYMAJ usuwanie prefixów!
                        // Nie usuwaj NICZEGO, zwróć oryginalny tekst
                        return text;
                    }
                }
            }

            // Usuń przedrostki ulic (istniejący kod bez zmian)
            var sortedPrefixes = StreetPrefixes.OrderByDescending(p => p.Length);

            foreach (var prefix in sortedPrefixes)
            {
                if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return text.Substring(prefix.Length + 1).Trim();
                }

                if (prefix.EndsWith(".") && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var afterPrefix = text.Substring(prefix.Length);
                    if (afterPrefix.Length > 0 && char.IsLetter(afterPrefix[0]))
                    {
                        return afterPrefix.Trim();
                    }
                }

                if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
            }

            return text;
        }

        private string RemoveTrailingNumbers(string text, out string extractedNumber)
        {
            extractedNumber = string.Empty;

            // Znajdź ostatnie słowo - jeśli jest liczbą lub zaczyna się od liczby, wyciągnij je
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                var lastWord = words[^1];
                
                // Sprawdź czy ostatnie słowo to liczba lub zawiera cyfry na początku
                if (char.IsDigit(lastWord[0]) || lastWord.All(char.IsDigit))
                {
                    // WYJĄTEK: Nie usuwaj numerów z nazw jednostek wojskowych
                    // np. "Dywizjonu 303", "Pułku 72", "Batalionu 101"
                    if (words.Length >= 2)
                    {
                        var secondLastWord = words[^2].ToLowerInvariant();
                        
                        // Lista słów kluczowych jednostek wojskowych
                        var militaryUnits = new[]
                        {
                            "dywizjonu", "dywizjon",
                            "pulku", "pułku", "pułk", "pulk",
                            "batalionu", "batalion",
                            "regimentu", "regiment",
                            "brygady",
                            "kompanii"
                        };

                        if (militaryUnits.Contains(secondLastWord))
                        {
                            // Nie wyciągaj numeru - jest częścią nazwy ulicy
                            return text;
                        }
                    }
                    
                    extractedNumber = lastWord;
                    return string.Join(" ", words.Take(words.Length - 1));
                }
            }

            return text;
        }
        
        /// <summary>
        /// Usuwa tytuły wojskowe, religijne, naukowe z tekstu
        /// </summary>
        private string RemoveTitles(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            // Lista tytułów do usunięcia (znormalizowane)
            var titles = new[] { 
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

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var filtered = words.Where(w => !titles.Contains(w)).ToList();
            
            return string.Join(" ", filtered);
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
            normalized = RemoveStreetPrefixes(normalized);
            
            // Usuń numery i zwróć je
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

        /// <summary>
        /// ✅ DODAJ TĘ METODĘ:
        /// Usuwa inicjały imion z nazw ulic (np. "G. Zapolskiej" -> "Zapolskiej")
        /// </summary>
        public string RemoveNameInitial(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Wzorzec: 1-3 litery + kropka + spacja (lub 1-3 litery + spacja)
            // Przykłady: "G. ", "Gen. ", "J.K. ", "dr ", "prof. "
            var pattern = @"^(?:[A-Za-zĄĆĘŁŃÓŚŹŻąćęłńóśźż]{1,3}\.?\s+)+";
            
            var result = System.Text.RegularExpressions.Regex.Replace(
                text, 
                pattern, 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return result.Trim();
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

        // ... reszta kodu
    }
}