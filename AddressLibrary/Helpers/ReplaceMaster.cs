namespace AddressLibrary.Helpers
{
    public static class ReplaceMaster
    {
        /// <summary>
        /// Zamienia wszystkie wystąpienia dosłownego tekstu na nowy (case-insensitive)
        /// Nie sprawdza granic słów - zamienia dokładnie to co znajduje
        /// </summary>
        public static string ReplaceStringIgnoreCase(string text, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(text))
                return text;

            // Własna implementacja Replace z case-insensitive (String.Replace ma bug)
            var result = new System.Text.StringBuilder(text.Length);
            int startIndex = 0;

            while (startIndex < text.Length)
            {
                int index = text.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);

                if (index == -1)
                {
                    // Brak więcej wystąpień - dodaj resztę tekstu
                    result.Append(text.AsSpan(startIndex));
                    break;
                }

                // Dodaj tekst przed znalezionym wzorcem
                result.Append(text.AsSpan(startIndex, index - startIndex));

                // Dodaj nową wartość
                result.Append(newValue);

                // Przesuń indeks za znaleziony wzorzec
                startIndex = index + oldValue.Length;
            }

            return result.ToString();
        }

        /// <summary>
        /// Zamienia wystąpienia tekstu TYLKO gdy występuje jako całe słowo (z granicami)
        /// Sprawdza czy wzorzec jest otoczony spacjami lub znajduje się na początku/końcu tekstu
        /// </summary>
        public static string ReplaceWordIgnoreCase(string text, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder(text.Length);
            int textIndex = 0;

            while (textIndex < text.Length)
            {
                // Znajdź następne wystąpienie wzorca (case-insensitive)
                int matchIndex = text.IndexOf(oldValue, textIndex, StringComparison.OrdinalIgnoreCase);

                if (matchIndex == -1)
                {
                    // Brak więcej dopasowań - skopiuj resztę tekstu
                    result.Append(text.AsSpan(textIndex));
                    break;
                }

                int matchEnd = matchIndex + oldValue.Length;

                // Sprawdź granice słowa:
                // - Na początku: matchIndex == 0 LUB poprzedni znak to spacja/interpunkcja
                // - Na końcu: matchEnd == text.Length LUB następny znak to spacja/interpunkcja
                bool isWordStart = matchIndex == 0 || IsWordBoundary(text[matchIndex - 1]);
                bool isWordEnd = matchEnd >= text.Length || IsWordBoundary(text[matchEnd]);

                // Skopiuj tekst przed dopasowaniem
                result.Append(text.AsSpan(textIndex, matchIndex - textIndex));

                if (isWordStart && isWordEnd)
                {
                    // To całe słowo - zamień
                    result.Append(newValue);
                    textIndex = matchEnd;
                }
                else
                {
                    // To nie całe słowo - skopiuj oryginał i przejdź dalej
                    result.Append(text.AsSpan(matchIndex, oldValue.Length));
                    textIndex = matchEnd;
                }
            }

            return result.ToString();
        }

        public static bool IsWordBoundary(char c)
        {
            return char.IsWhiteSpace(c) ||
                   c == '.' || c == ',' || c == ';' || c == ':' ||
                   c == '-' || c == '/' || c == '\\' ||
                   c == '(' || c == ')' || c == '[' || c == ']' ||
                   c == '{' || c == '}' || c == '"';
            // Uwaga: tutaj usunąłem pojedynczy apostrof używany w Tagore'a czyli apostrofy są teraz częścią słów
        }
    }
}
