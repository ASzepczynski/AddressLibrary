using AddressLibrary.Models;
using AddressLibrary.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AddressLibrary.Helpers
{
    static public class UliceUtils
    {

        static public string Wesola(ResultList ulic)
        {
            // Wyjątek dla Wesołej, dzielnicy Warszawy. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę
            if (ulic.WojewodztwoNazwa.ToLower() == "mazowieckie"
                && ulic.PowiatNazwa == "Warszawa"
                && ulic.GminaNazwa == "Wesoła"
                && ulic.Miasto.Nazwa == "Wesoła"
                && ulic.Miasto.RodzajMiasta == "95")
            {
                return "Wesoła";
            }
            return "";
        }
        static public (string ulicaNazwa, string dzielnicaNazwa) ZielonaGora(Miasto miasto, string sUlica, string sDzielnica)
        {
            var dzielnice = new List<string> {
                        "Drzonków",
                        "Kiełpin",
                        "Kisielin",
                        "Krępa",
                        "Łężyca",
                        "Ługowo",
                        "Nowy Kisielin",
                        "Ochla",
                        "Przylep",
                        "Racula",
                        "Stary Kisielin",
                        "Zatonie",
                        "Zawada"
                    };
            string ulicaNazwa = sUlica;
            string dzielnicaNazwa = sDzielnica;

            // Wyjątek dla Zielonej Góry. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę, która jest zawarta w nazwie ulicy.
            if (miasto.Gmina.Powiat.Wojewodztwo.Nazwa.ToLower() == "lubuskie"
                && miasto.Gmina.Powiat.Nazwa == "Zielona Góra"
                && miasto.Gmina.Nazwa == "Zielona Góra"
                && miasto.Nazwa == "Zielona Góra")
            {
                foreach (var dziel in dzielnice)
                {
                    if (sUlica.StartsWith(dziel + "-"))
                    {
                        dzielnicaNazwa = dziel;
                        ulicaNazwa = sUlica.Remove(0, dziel.Length + 1);
                        break;
                    }
                }
            }
            return (ulicaNazwa, dzielnicaNazwa);
        }

        static public (string Nazwa1, string Nazwa2) GetCorrectedStreetName(string Nazwa1, string Nazwa2)
        {
            Nazwa2 = Nazwa2.Replace("-go", "");
            Nazwa1 = Nazwa1.Replace("-go", "");
            // ✅ OBSŁUGA ULIC Z NUMEREM (np. "3 Maja")
            // Jeśli Nazwa2 wygląda jak liczba/data → zamień Nazwa1
            // na "Nazwa2 Nazwa1"
            if (!string.IsNullOrEmpty(Nazwa2) && IsNumericPrefix(Nazwa2))
            {
                return ($"{Nazwa2} {Nazwa1}".Trim(), "");
            }

            if ((Nazwa2 == "Księcia") && Nazwa1 == "Józefa")
            {
                return ($"{Nazwa2} {Nazwa1}".Trim(), "");
            }

            return (Nazwa1.Trim(), Nazwa2.Trim());

        }
        /// <summary>
        /// Sprawdza czy Nazwa2 to prefix numeryczny/datowy
        /// Przykłady: "3-go", "1", "29", "15-go", "II", "1-go"
        /// </summary>
        static public bool IsNumericPrefix(string nazwa2)
        {
            if (string.IsNullOrWhiteSpace(nazwa2))
                return false;

            // Usuń białe znaki
            var trimmed = nazwa2.Trim();

            // ✅ WZORCE DLA NAZW NUMERYCZNYCH:
            // 1. Zawiera cyfry: "3-go", "29", "1-go", "15"
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\d"))
                return true;

            // 2. Numery rzymskie: "II", "III", "IV"
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(I|V|X|L|C|D|M)+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Normalizuje liczebniki porządkowe (usuwa "-go", "-tego", "-cie")
        /// </summary>
        public static string NormalizeOrdinalNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"-?(go|tego|cie)$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Trim();
        }

        public static readonly Dictionary<string, List<string>> StreetPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "aleja",    new List<string> { "al.", "al", "aleja" } },
            { "bulwar",   new List<string> { "bulw.", "bulwar"} },
            { "droga",    new List<string> { "droga" } },
            { "ogród",    new List<string> { "ogród"}},
            { "osiedle",    new List<string> { "os.", "os", "oś.", "oś","osiedle" } },
            { "park",     new List<string> { "park" } },
            { "pasaż",    new List<string> { "pasaż"}},
            { "plac",     new List<string> { "pl.", "plac","pl" } },
            { "rondo",    new List<string> { "rondo" } },
            { "rynek",    new List<string> { "rynek"}},
            { "skwer",    new List<string> { "skw.", "skwer"} },
            { "szosa",    new List<string> { "szosa" } },
            { "ścieżka",  new List<string> { "ścieżka"} },
            { "ulica",    new List<string> { "ul.", "ul", "ulica" } }
        };
        /// <summary>
        /// Zwraca preferowany skrót dla typu ulicy (np. "aleja" → "al.", "plac" → "pl.")
        /// Korzysta z pierwszego wariantu ze słownika StreetPrefixes jako preferowanego skrótu
        /// </summary>
        /// <param name="text">Nazwa typu ulicy (np. "aleja", "al.", "plac", "ulica")</param>
        /// <returns>Preferowany skrót (pierwszy wariant ze słownika) lub oryginalny tekst</returns>
        public static string GetStreetAbbreviation(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalized = text.Trim();

            // Znajdź klucz w słowniku, który zawiera podany wariant
            foreach (var entry in StreetPrefixes)
            {
                if (entry.Value.Any(v => v.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    // Zwróć pierwszy wariant ze słownika (zawsze skrót, np. "al.", "pl.")
                    return entry.Value[0];
                }
            }

            // Nie znaleziono - zwróć oryginalny
            return text;
        }

        public static string RemoveStreetPrefixes(string text)
        {
            var sortedPrefixes = StreetPrefixes
                .SelectMany(kv => kv.Value).
                Distinct(StringComparer.OrdinalIgnoreCase).
                ToList()
                .OrderByDescending(p => p.Length);

            foreach (var prefix in sortedPrefixes)
            {
                if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return text.Substring(prefix.Length + 1).Trim();
                }

                if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
            }

            return text;
        }

        /// <summary>
        /// Normalizuje kod pocztowy do formatu XX-XXX
        /// </summary>
        public static string NormalizujKodPocztowy(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return string.Empty;
            }

            // Usuń wszystko oprócz cyfr
            var cyfry = new string(kod.Where(char.IsDigit).ToArray());

            if (cyfry.Length != 5)
            {
                return kod; // Zwróć oryginalny jeśli nieprawidłowy format
            }

            return $"{cyfry.Substring(0, 2)}-{cyfry.Substring(2, 3)}";
        }

        /// <summary>
        /// Rozdziela nazwę ulicy na prefiks (cechę) i właściwą nazwę
        /// Zwraca znormalizowany prefiks (pierwszy wariant ze słownika) i pozostałą część nazwy
        /// </summary>
        /// <param name="sUlica">Pełna nazwa ulicy (np. "aleja Jana Pawła II", "pl. Wolności")</param>
        /// <returns>Tuple (znormalizowany prefiks lub null, nazwa bez prefiksu)</returns>
        public static (string Prefix, string Name) SplitStreetAndPrefix(string sUlica)
        {
            if (string.IsNullOrWhiteSpace(sUlica))
                return ("", sUlica ?? string.Empty);

            var trimmed = sUlica.Trim();

            // Pobierz wszystkie prefiksy posortowane malejąco (najdłuższe najpierw)
            // aby uniknąć fałszywych dopasowań (np. "al." przed "aleja")
            var sortedPrefixes = StreetPrefixes
                .SelectMany(kv => kv.Value.Select(v => new { Key = kv.Key, Value = v }))
                .OrderByDescending(p => p.Value.Length)
                .ToList();

            foreach (var prefixEntry in sortedPrefixes)
            {
                var prefixWithSpace = prefixEntry.Value + " ";

                // Sprawdź czy ulica zaczyna się od prefiksu ze spacją
                if (trimmed.StartsWith(prefixWithSpace, StringComparison.OrdinalIgnoreCase))
                {
                    var remainingName = trimmed.Substring(prefixWithSpace.Length).Trim();
                    var normalizedPrefix = StreetPrefixes[prefixEntry.Key][0]; // Pierwszy wariant (znormalizowany)
                    return (normalizedPrefix, remainingName);
                }

                // Sprawdź czy cała nazwa to tylko prefiks (np. "Rynek")
                if (trimmed.Equals(prefixEntry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedPrefix = StreetPrefixes[prefixEntry.Key][0];
                    return (normalizedPrefix, string.Empty);
                }
            }

            // Nie znaleziono prefiksu - zwróć oryginalną nazwę
            return ("", trimmed);
        }
        public static string RemoveDiacritics(string text)
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
            return ZamienPolskie(stringBuilder.ToString());

            //  Litera ł(U+0142) i Ł(U+0141) są osobnymi znakami w Unicode, a nie literą bazową z nałożonym znakiem diakrytycznym.
            // 	Standardowa normalizacja Unicode(FormD) i usuwanie znaków diakrytycznych działa dla znaków takich jak: ą → a, ć → c, é → e, ö → o, itp., ale nie zamienia ł na l ani Ł na L.
        }


        // Zamienia polskie litery na łacińskie
        // Funkcja RemoveDiacritics miała problemy z 'ł' i z 'ż'

        public static string ZamienPolskie(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var replacements = new Dictionary<char, char>
    {
        { 'ą', 'a' }, { 'ć', 'c' }, { 'ę', 'e' }, { 'ł', 'l' }, { 'ń', 'n' },
        { 'ó', 'o' }, { 'ś', 's' }, { 'ź', 'z' }, { 'ż', 'z' },
        { 'Ą', 'A' }, { 'Ć', 'C' }, { 'Ę', 'E' }, { 'Ł', 'L' }, { 'Ń', 'N' },
        { 'Ó', 'O' }, { 'Ś', 'S' }, { 'Ź', 'Z' }, { 'Ż', 'Z' }
    };

            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                sb.Append(replacements.TryGetValue(c, out var ascii) ? ascii : c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// ✅ DODAJ TĘ METODĘ:
        /// Usuwa inicjały imion z nazw ulic (np. "G. Zapolskiej" -> "Zapolskiej")
        /// </summary>
        public static string RemoveNameInitial(string text)
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

        /// <summary>
        /// Buduje pełną nazwę ulicy z Nazwa2 (prefiks) + Nazwa1 (główna nazwa)
        /// </summary>
        public static string GetPelnaNazwa(Ulica ulica)
        {
            if (string.IsNullOrEmpty(ulica.Nazwa2))
            {
                return ulica.Nazwa1;
            }
            return $"{ulica.Nazwa2} {ulica.Nazwa1}";
        }
        public static List<string> GetAllStreetPrefixes()
        {
            return StreetPrefixes
                .SelectMany(kv => kv.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => p.Length)
                .ToList();
        }

        public static (string? Prefix, string Name) SplitStreetPrefix(string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return (null, streetName);

            // Pobierz wszystkie możliwe prefiksy, posortowane malejąco po długości (żeby najpierw sprawdzić najdłuższe)
            var allPrefixes = GetAllStreetPrefixes();

            foreach (var prefix in allPrefixes)
            {
                var prefixWithSpace = prefix + " ";
                if (streetName.StartsWith(prefixWithSpace, StringComparison.OrdinalIgnoreCase))
                {
                    // Znajdź pełną nazwę cechy na podstawie słownika
                    var fullType = StreetPrefixes.FirstOrDefault(kv => kv.Value.Any(v => v.Equals(prefix, StringComparison.OrdinalIgnoreCase))).Key;
                    return (prefix, streetName.Substring(prefixWithSpace.Length).TrimStart());
                }
                if (streetName.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var fullType = StreetPrefixes.FirstOrDefault(kv => kv.Value.Any(v => v.Equals(prefix, StringComparison.OrdinalIgnoreCase))).Key;
                    return (prefix, string.Empty);
                }
            }

            return (null, streetName);
        }
        public static string RemoveStreetTypeDuplication(string streetType, string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetType) || string.IsNullOrWhiteSpace(streetName))
                return streetName;

            // Znajdź pełną nazwę typu na podstawie streetType (może być skrótem lub pełną nazwą)
            string? fullType = StreetPrefixes
                .FirstOrDefault(kv => kv.Value.Any(v => v.Equals(streetType, StringComparison.OrdinalIgnoreCase)
                                                     || kv.Key.Equals(streetType, StringComparison.OrdinalIgnoreCase)))
                .Key;

            if (fullType == null)
                return streetName;

            // Pobierz wszystkie warianty prefiksu dla danego typu
            var allVariants = StreetPrefixes[fullType];

            // Sprawdź, czy streetName zaczyna się od dowolnego wariantu (np. "aleja", "al.", "al")
            foreach (var variant in allVariants.OrderByDescending(v => v.Length))
            {
                var variantWithSpace = variant + " ";
                if (streetName.StartsWith(variantWithSpace, StringComparison.OrdinalIgnoreCase))
                {
                    // Usuń prefiks i zwróć resztę
                    return streetName.Substring(variantWithSpace.Length).TrimStart();
                }
            }

            return streetName;
        }
        
        public static (string street, string houseNumber) ExtractHouseNumberFromStreet(string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return (streetName, "");

            // Regex dopasowujący numer na końcu ulicy
            // Przykłady: "ul.1 Maja 52", "3Maja 126b", "A.Krajowej 7"
            var match = System.Text.RegularExpressions.Regex.Match(
                streetName,
                @"^(.+?)\s+(\d+[a-zA-Z]?)$",
                System.Text.RegularExpressions.RegexOptions.RightToLeft
            );

            if (match.Success)
            {
                var street = match.Groups[1].Value.Trim();
                var number = match.Groups[2].Value.Trim();
                return (street, number);
            }

            return (streetName, "");
        }
    }
}
