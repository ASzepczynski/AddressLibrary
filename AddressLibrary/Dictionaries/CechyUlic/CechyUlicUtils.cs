namespace AddressLibrary.Dictionaries.CechyUlic
{
    /// <summary>
    /// Narzędzia do pracy z cechami ulic (prefiksy, skróty, normalizacja)
    /// </summary>
    public static class CechyUlicUtils
    {
        public static readonly Dictionary<string, List<string>> StreetPrefixes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Czy słownik prefiksów ulic został zainicjalizowany danymi z bazy
        /// </summary>
        public static bool IsInitialized => StreetPrefixes.Count > 0;

        /// <summary>
        /// Sprawdza czy słownik został zainicjalizowany. Jeśli nie - rzuca InvalidOperationException.
        /// Należy wywołać przed każdą operacją zależną od StreetPrefixes.
        /// </summary>
        /// <exception cref="InvalidOperationException">Gdy StreetPrefixes jest pusty</exception>
        public static void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException(
                    "CechyUlicUtils nie zostały zainicjalizowane. " +
                    "Przed użyciem należy załadować dane z bazy wywołując " +
                    "CechyUlicUtils.Add() dla każdej cechy ulicy (tabela CechaUlicy). " +
                    "Inicjalizacja odbywa się w LoadTypyUlicService lub analogicznym serwisie startowym.");
        }

        /// <summary>
        /// Dodaje nową cechę ulicy do słownika StreetPrefixes
        /// </summary>
        /// <param name="Cecha">Klucz - pełna nazwa cechy (np. "aleja", "ulica", "plac")</param>
        /// <param name="Lista">Lista wariantów tej cechy (np. ["al.", "al", "aleja"]). 
        /// UWAGA: Pierwszy element listy powinien być preferowanym skrótem (np. "al.")</param>
        /// <example>
        /// CechyUlicUtils.Add("aleja", new List&lt;string&gt; { "al.", "al", "aleja" });
        /// // Dodaje do słownika: ["aleja"] -> ["al.", "al", "aleja"]
        /// // gdzie "al." jest preferowanym skrótem (używanym przez GetStreetAbbreviation)
        /// </example>
        public static void Add(string Cecha, List<string> Lista)
        {
            // Walidacja - sprawdź czy parametry nie są puste
            if (string.IsNullOrWhiteSpace(Cecha) || Lista == null || Lista.Count == 0)
            {
                throw new ArgumentException("Cecha i Lista nie mogą być puste");
            }

            // Sprawdź czy klucz już istnieje w słowniku
            if (StreetPrefixes.ContainsKey(Cecha))
            {
                // Jeśli istnieje, zaktualizuj listę wariantów
                StreetPrefixes[Cecha] = Lista;
            }
            else
            {
                // Jeśli nie istnieje, dodaj nowy wpis do słownika
                // Dictionary.Add(klucz, wartość) - dodaje parę klucz-wartość
                // klucz: pełna nazwa cechy (np. "aleja")
                // wartość: lista wszystkich wariantów (np. ["al.", "al", "aleja"])
                StreetPrefixes.Add(Cecha, Lista);
            }
        }

        /// <summary>
        /// Zwraca preferowany skrót dla typu ulicy (np. "aleja" → "al.", "plac" → "pl.")
        /// Korzysta z pierwszego wariantu ze słownika StreetPrefixes jako preferowanego skrótu
        /// </summary>
        /// <param name="text">Nazwa typu ulicy (np. "aleja", "al.", "plac", "ulica")</param>
        /// <returns>Preferowany skrót (pierwszy wariant ze słownika) lub oryginalny tekst</returns>
        public static string GetStreetAbbreviation(string text)
        {
            EnsureInitialized();
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

        /// <summary>
        /// Usuwa prefiksy ulic z tekstu (np. "ul. Główna" → "Główna")
        /// </summary>
        public static string RemoveStreetPrefixes(string text)
        {
            EnsureInitialized();
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
        /// Zwraca listę wszystkich prefiksów ulic posortowaną malejąco po długości
        /// </summary>
        public static List<string> GetAllStreetPrefixes()
        {
            EnsureInitialized();
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
            EnsureInitialized();
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

        /// <summary>
        /// Rozdziela prefiks z nazwy ulicy pochodzącej z TERYTu (usuwa "inne" na początku)
        /// </summary>
        public static (string prefiks, string reszta) RozdzielPrefiksTeryt(string ulica)
        {
            // Usuń Inne na początku
            if (ulica.StartsWith("inne "))
            {
                ulica = ulica.Substring(5).Trim();
            }

            var (prefix, name) = SplitStreetPrefix(ulica);

            // sprawdzamy czy coś jeszcze zostało w ulicy, na przykład rondo Rondo
            var (prefix2, name2) = SplitStreetPrefix(name);

            if (prefix2 != "")
            {
                return (prefix2, name2);
            }

            return (prefix, name);
        }

        /// <summary>
        /// Usuwa duplikację typu ulicy (np. "aleja" + "aleja Główna" → "Główna")
        /// </summary>
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
        /// Zamienia wszystkie spacje i znaki specjalne na '%' dla wzorca LIKE
        /// Przykład: "Boh. Września" -> "Boh%Wrzesnia%"
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
    }
}