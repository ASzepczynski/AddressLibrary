using AddressLibrary.Models;
using AddressLibrary.Structures;
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
            { "aleja",    new List<string> { "al.", "al", "aleje", "aleja" } },
            { "bulwar",   new List<string> { "bulw.", "bulwar"} },
            { "bulwary",   new List<string> { "bulw.", "bulwary"} },
            { "droga",    new List<string> { "droga" } },
            { "most",    new List<string> { "most" } },
            { "ogród",    new List<string> { "ogród"}},
            { "osiedle",    new List<string> { "os.", "os", "oś.", "oś","osiedle" } },
            { "park",     new List<string> { "park" } },
            { "pasaż",    new List<string> { "pasaż"}},
            { "plac",     new List<string> { "pl.", "pl", "plac" } },
            { "rondo",    new List<string> { "rondo" } },
            { "rynek",    new List<string> { "rynek"}},
            { "skwer",    new List<string> { "skw.", "skw", "skwer"} },
            { "szosa",    new List<string> { "szosa" } },
            { "ścieżka",  new List<string> { "ścieżka"} },
            { "ulica",    new List<string> { "ul.", "ul", "ulica" } },
            { "wybrzeże",    new List<string> { "wyb.", "wyb", "wybrzeże" } },
            { "nabrzeże",    new List<string> { "nab.", "nab", "nabrzeże" } }
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
            var pattern = @"^(?:[A-Za-zĄĆĘŁŃÓŚŹŻ]{1,3}\.?\s+)+";

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
        /// <summary>
        /// Buduje pełną nazwę ulicy z Nazwa2 (prefiks) + Nazwa1 (główna nazwa)
        /// </summary>
        public static string GetPelnaNazwaZPrefiksem(Ulica ulica)
        {
            var x = GetPelnaNazwa(ulica);
            return $"{ulica.Cecha} {x}".Trim();
        }
        public static List<string> GetAllStreetPrefixes()
        {
            return StreetPrefixes
                .SelectMany(kv => kv.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => p.Length)
                .ToList();
        }


        /// <summary>
        /// Rozdziela nazwę ulicy na prefiks (cechę) i właściwą nazwę
        /// Zwraca znormalizowany prefiks (pierwszy wariant ze słownika) i pozostałą część nazwy
        /// </summary>
        /// <param name="streetName">Pełna nazwa ulicy (np. "aleja Jana Pawła II", "pl. Wolności")</param>
        /// <returns>Tuple (znormalizowany prefiks (pierwszy ze słownika) lub pusty string, nazwa bez prefiksu)</returns>
        public static (string Prefix, string Name) SplitStreetPrefix(string? streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return ("", streetName ?? string.Empty);

            // Pobierz wszystkie możliwe prefiksy, posortowane malejąco po długości (żeby najpierw sprawdzić najdłuższe)
            var allPrefixes = GetAllStreetPrefixes();

            foreach (var prefix in allPrefixes)
            {
                var prefixWithSpace = prefix + " ";
                if (streetName.StartsWith(prefixWithSpace, StringComparison.OrdinalIgnoreCase))
                {
                    // Znajdź klucz słownika i zwróć PIERWSZY wariant (znormalizowany skrót)
                    var entry = StreetPrefixes.FirstOrDefault(kv =>
                        kv.Value.Any(v => v.Equals(prefix, StringComparison.OrdinalIgnoreCase)));

                    if (!string.IsNullOrEmpty(entry.Key))
                    {
                        var normalizedPrefix = entry.Value[0]; // ✅ Pierwszy wariant (np. "al.", "ul.", "pl.")
                        return (normalizedPrefix, streetName.Substring(prefixWithSpace.Length).TrimStart());
                    }
                }
                // Nie chcemy zostawiać nazwy ulicy pustej czyli przypadku "Rynek" czy "Osiedle"
            }
            return ("", streetName);
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
        /// Porównuje string z wzorcem podobnie do LIKE w SQL.
        /// Automatycznie zamienia wszystkie spacje i znaki specjalne na wildcard '%'.
        /// Przykłady:
        /// - "Boh. Września" -> "Boh%Wrzes%" pasuje do "Bohaterów Września"
        /// - "Bat.Chłopskich" -> "Bat%Chłopskich" pasuje do "Batalionów Chłopskich"
        /// </summary>
        /// <param name="input">String do sprawdzenia</param>
        /// <param name="pattern">Wzorzec (będzie przekształcony na wzorzec LIKE)</param>
        /// <returns>True jeśli input pasuje do wzorca</returns>
        public static bool IsLikePattern(string input, string pattern)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
                return false;

            // ✅ KROK 1: Znormalizuj oba stringi (lowercase, bez diakrytyków)
            var normalizedInput = NormalizeForPattern(input);
            var normalizedPattern = NormalizeForPattern(pattern);

            // ✅ KROK 2: Zamień wszystkie spacje i znaki specjalne na '%'
            var wildcardPattern = ConvertToWildcardPattern(normalizedPattern);

            // ✅ KROK 3: Przekształć wzorzec SQL LIKE na regex
            var regexPattern = ConvertLikeToRegex(wildcardPattern);

            // ✅ KROK 4: Sprawdź dopasowanie
            return System.Text.RegularExpressions.Regex.IsMatch(
                normalizedInput,
                regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
        /// Zamienia wszystkie spacje i znaki specjalne na '%'
        /// Przykład: "Boh. Września" -> "Boh%Wrzesnia"
        /// </summary>
        public static string ConvertToWildcardPattern(string pattern)
        {
            var result = new System.Text.StringBuilder();
            bool lastWasWildcard = false;

            foreach (char c in pattern)
            {
                // ✅ Jeśli to litera lub cyfra - dodaj do wzorca
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(c);
                    lastWasWildcard = false;
                }
                // ✅ Jeśli to spacja, kropka, myślnik, ukośnik itp. - zamień na '%'
                else if (char.IsWhiteSpace(c) || c == '.' || c == '-' || c == '/' || c == ',' || c == ';')
                {
                    if (!lastWasWildcard) // Unikaj duplikacji '%%'
                    {
                        result.Append('%');
                        lastWasWildcard = true;
                    }
                }
                // ✅ Inne znaki specjalne - zachowaj wildcard
                else if (c == '%' || c == '_')
                {
                    result.Append(c);
                    lastWasWildcard = (c == '%');
                }
            }
            result.Append("%");
            return result.ToString();
        }

        /// <summary>
        /// ⚡ SZYBKA funkcja sprawdzająca czy wzorzec pasuje do tekstu od lewej do prawej
        /// Usuwa wszystkie znaki oprócz liter i cyfr, następnie szuka każdej litery wzorca w tekście po kolei.
        /// Przykłady:
        /// - "Bat.Chłopskich" pasuje do "Batalionów Chłopskich" ✅
        /// - "Boh.Września" pasuje do "Bohaterów Września" ✅
        /// </summary>
        public static bool IsLeftToRightMatch(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return false;

            // KROK 1: Znormalizuj oba stringi (usuń diakrytyki, lowercase)
            var normalized1 = UliceUtils.RemoveDiacritics(str1.ToLowerInvariant());
            var normalized2 = UliceUtils.RemoveDiacritics(str2.ToLowerInvariant());

            // KROK 2: Usuń wszystkie znaki oprócz liter i cyfr
            var clean1 = new string(normalized1.Where(c => char.IsLetterOrDigit(c)).ToArray());
            var clean2 = new string(normalized2.Where(c => char.IsLetterOrDigit(c)).ToArray());

            // KROK 3: Automatycznie wykryj który jest wzorcem (krótszy) a który tekstem (dłuższy)
            string pattern, text;
            if (clean1.Length <= clean2.Length)
            {
                pattern = clean1;
                text = clean2;
            }
            else
            {
                pattern = clean2;
                text = clean1;
            }

            // KROK 4: Dla każdej litery z wzorca znajdź pierwsze wystąpienie w tekście
            int textIndex = 0;

            foreach (char patternChar in pattern)
            {
                // Znajdź pierwszą pozycję tej litery w pozostałej części tekstu
                int foundIndex = text.IndexOf(patternChar, textIndex);

                if (foundIndex == -1)
                {
                    // Nie znaleziono litery - brak dopasowania
                    return false;
                }

                // Przesuń indeks za znalezioną literę
                textIndex = foundIndex + 1;
            }

            // Sukces - znaleziono wszystkie litery wzorca w odpowiedniej kolejności
            return true;
        }
        /// <summary>
        /// Konwertuje wzorzec SQL LIKE (z % i _) na regex
        /// % = dowolna ilość znaków (.*?)
        /// _ = dokładnie jeden znak (.)
        /// </summary>
        public static string ConvertLikeToRegex(string likePattern)
        {
            var regex = new System.Text.StringBuilder("^");

            foreach (char c in likePattern)
            {
                switch (c)
                {
                    case '%':
                        regex.Append(".*?"); // Non-greedy match
                        break;
                    case '_':
                        regex.Append("."); // Dokładnie jeden znak
                        break;
                    case '.':
                    case '*':
                    case '+':
                    case '?':
                    case '|':
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case '^':
                    case '$':
                    case '\\':
                        regex.Append('\\').Append(c); // Escape regex special chars
                        break;
                    default:
                        regex.Append(c);
                        break;
                }
            }

            regex.Append('$');
            return regex.ToString();
        }

        /// <summary>
        /// Poprawia cudzysłowy w tekstach CSV - usuwa zewnętrzne i konwertuje podwójne na pojedyncze
        /// Przykład: "Fieldorfa ""Nila""" -> Fieldorfa "Nila"
        /// </summary>
        public static string RemoveQuote(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            var result = text.Trim();

            // 1. Usuń zewnętrzne cudzysłowy (początkowy i końcowy)
            if (result.StartsWith("\"") && result.EndsWith("\""))
            {
                result = result.Substring(1, result.Length - 2);
            }

            // 2. Zamień podwójne cudzysłowy ("") na pojedyncze (")
            result = result.Replace("\"\"", "\"");

            return result;
        }
    }
}
