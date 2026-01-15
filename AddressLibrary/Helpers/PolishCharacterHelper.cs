// Copyright (c) 2025-2026 Andrzej SzepczyÒski. All rights reserved.

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Klasa pomocnicza do obs≥ugi polskich znakÛw diakrytycznych
    /// </summary>
    public static class PolishCharacterHelper
    {
        /// <summary>
        /// Zamienia polskie znaki diakrytyczne na ich odpowiedniki ASCII
        /// </summary>
        /// <param name="text">Tekst do przetworzenia</param>
        /// <returns>Tekst z zamienionymi polskimi znakami na ASCII</returns>
        public static string RemovePolishDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace('π', 'a').Replace('•', 'A')
                .Replace('Ê', 'c').Replace('∆', 'C')
                .Replace('Í', 'e').Replace(' ', 'E')
                .Replace('≥', 'l').Replace('£', 'L')
                .Replace('Ò', 'n').Replace('—', 'N')
                .Replace('Û', 'o').Replace('”', 'O')
                .Replace('ú', 's').Replace('å', 'S')
                .Replace('ü', 'z').Replace('è', 'Z')
                .Replace('ø', 'z').Replace('Ø', 'Z');
        }

        /// <summary>
        /// Sprawdza czy tekst zawiera polskie znaki diakrytyczne
        /// </summary>
        /// <param name="text">Tekst do sprawdzenia</param>
        /// <returns>True jeúli tekst zawiera polskie znaki</returns>
        public static bool ContainsPolishCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Any(c => "πÊÍ≥ÒÛúüø•∆ £—”åèØ".Contains(c));
        }

        /// <summary>
        /// Normalizuje tekst pod kπtem porÛwnywania (usuwa polskie znaki + lower case)
        /// </summary>
        /// <param name="text">Tekst do znormalizowania</param>
        /// <returns>Znormalizowany tekst</returns>
        public static string NormalizeForComparison(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return RemovePolishDiacritics(text).ToLowerInvariant();
        }
    }
}