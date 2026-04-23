using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Models;
using AddressLibrary.Structures;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using System.Text;

namespace AddressLibrary.Helpers
{
    static public class UliceUtils
    {

        static public List<string> dzielnice_zg = new List<string> {
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
            
            string ulicaNazwa = sUlica;
            string dzielnicaNazwa = sDzielnica;

            // Wyjątek dla Zielonej Góry. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę, która jest zawarta w nazwie ulicy.
            if (miasto.Gmina.Powiat.Wojewodztwo.Nazwa.ToLower() == "lubuskie"
                && miasto.Gmina.Powiat.Nazwa == "Zielona Góra"
                && miasto.Gmina.Nazwa == "Zielona Góra"
                && miasto.Nazwa == "Zielona Góra")
            {
                foreach (var dziel in dzielnice_zg)
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

       
        /// <summary>
        /// Normalizuje liczebniki porządkowe (usuwa "-go")
        /// </summary>
        public static string NormalizeOrdinalNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"-?(go)$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Trim();
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
                return string.Empty; // ✅ POPRAWKA: Zwróć pusty string zamiast oryginalnego kodu
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
            return PolishUtils.ToLatin(stringBuilder.ToString());

            //  Litera ł(U+0142) i Ł(U+0141) są osobnymi znakami w Unicode, a nie literą bazową z nałożonym znakiem diakrytycznym.
            // 	Standardowa normalizacja Unicode(FormD) i usuwanie znaków diakrytycznych działa dla znaków takich jak: ą → a, ć → c, é → e, ö → o, itp., ale nie zamienia ł na l ani Ł na L.
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
       

        /// <summary>
        /// Wyodrębnia numer domu z końca nazwy ulicy
        /// Obsługuje formaty: "52", "126b", "25a/87", "10/12"
        /// </summary>
        public static (string street, string houseNumber) ExtractHouseNumberFromStreet(string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return (streetName, "");

            // ✅ Rozszerzony regex dopasowujący różne formaty numerów:
            // - Prosty numer: "52"
            // - Z literą: "126b", "25a"
            // - Z ukośnikiem: "25/87", "25a/87", "10/12"
            // Przykłady: "ul.1 Maja 52", "3Maja 126b", "A.Krajowej 7", "Główna 25a/87"
            //
            // Poprawiłem by nie było więcej niż 3 cyfry, bo kradło lata 1863r i 1945
            //
            var match = System.Text.RegularExpressions.Regex.Match(
                streetName,
                @"^(.+?)\s+(\d{1,3}[a-zA-Z]?(?:/\d+[a-zA-Z]?)?)$",
                System.Text.RegularExpressions.RegexOptions.RightToLeft
            );

            if (!match.Success)
            {
                return (streetName, "");
            }
            var street = match.Groups[1].Value.Trim();
            var number = match.Groups[2].Value.Trim();

            // Unikamy obcięcia osiedla Dywizjonu 303 
            if (street.EndsWith("dywizjonu", StringComparison.OrdinalIgnoreCase))
            {
                return (streetName, "");
            }
            // Unikamy obcięcia Jana Pawła 2
            if (street.EndsWith("jana pawła", StringComparison.OrdinalIgnoreCase) && (number == "2"))
            {
                return (streetName, "");
            }
            return (street, number);
        }

        
        /// <summary>
        /// Normalizuje string do porównania (lowercase + usunięcie diakrytyków)
        /// </summary>
        private static string NormalizeForPattern(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Usuń diakrytyki i zamień na lowercase
            return UliceUtils.RemoveDiacritics(text.ToLowerInvariant());
        }
       

        /// <summary>
        /// Poprawia cudzysłowy w tekstach CSV - usuwa zewnętrzne i konwertuje podwójne na pojedyncze
        /// Przykład: "Fieldorfa ""Nila""" -> Fieldorfa "Nila"
        /// </summary>
        public static string RemoveQuote(string text)
        {
      
            return text.Replace("\"", "");

        }
    }
}
