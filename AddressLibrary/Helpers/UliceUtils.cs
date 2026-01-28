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
        static public (string Nazwa1, string dzielnica) ZielonaGoraWesola(ResultList ulic)
        {
            string Nazwa1 = ulic.Ulica.Nazwa1;
            string dzielnica = "";

            // Wyjątek dla Wesołej, dzielnicy Warszawy. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę
            if (ulic.WojewodztwoNazwa.ToLower() == "mazowieckie" && ulic.PowiatNazwa == "Warszawa" && ulic.GminaNazwa == "Wesoła" && ulic.Miasto?.Nazwa == "Wesoła" && ulic.Miasto.RodzajMiasta == "95")
            {
                dzielnica = "Wesoła";
            }
            // Wyjątek dla Zielonej Góry. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę, która jest zawarta w nazwie ulicy.

            if (ulic.WojewodztwoNazwa.ToLower() == "lubuskie" && ulic.PowiatNazwa == "Zielona Góra" && ulic.GminaNazwa == "Zielona Góra" && ulic.Miasto?.Nazwa == "Zielona Góra")
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


                foreach (var dziel in dzielnice)
                {
                    if (ulic.Ulica.Nazwa1.StartsWith(dziel + "-"))
                    {
                        dzielnica = dziel;
                        Nazwa1 = ulic.Ulica.Nazwa1.Remove(0, dziel.Length + 1);
                        break;
                    }
                }
            }
            return (Nazwa1, dzielnica);
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

        public static string RemoveStreetPrefixes(string text)
        {
            var sortedPrefixes = StreetPrefixes.OrderByDescending(p => p.Length);

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
            //  Litera ł(U+0142) i Ł(U+0141) są osobnymi znakami w Unicode, a nie literą bazową z nałożonym znakiem diakrytycznym.
            // 	Standardowa normalizacja Unicode(FormD) i usuwanie znaków diakrytycznych działa dla znaków takich jak: ą → a, ć → c, é → e, ö → o, itp., ale nie zamienia ł na l ani Ł na L.
            return stringBuilder.ToString().Replace('ł', 'l').Replace('Ł', 'L').Normalize(NormalizationForm.FormC);
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

    }
}
