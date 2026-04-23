using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Helpers
{


    public static class PolishUtils
    {
        static Dictionary<char, char> dictPolishLetters = new Dictionary<char, char>
    {
        { 'ą', 'a' }, { 'ć', 'c' }, { 'ę', 'e' }, { 'ł', 'l' }, { 'ń', 'n' },
        { 'ó', 'o' }, { 'ś', 's' }, { 'ź', 'z' }, { 'ż', 'z' },
        { 'Ą', 'A' }, { 'Ć', 'C' }, { 'Ę', 'E' }, { 'Ł', 'L' }, { 'Ń', 'N' },
        { 'Ó', 'O' }, { 'Ś', 'S' }, { 'Ź', 'Z' }, { 'Ż', 'Z' }
    };

        /// <summary>
        /// Zwraca string zawierający wszystkie polskie litery ze słownika replacements.
        /// </summary>
        public static string PolishLetters()
        {
            return new string(dictPolishLetters.Keys.ToArray());
        }

        // Zamienia polskie litery na łacińskie

        public static string ToLatin(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;


            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                sb.Append(dictPolishLetters.TryGetValue(c, out var ascii) ? ascii : c);
            }
            return sb.ToString();
        }
    }
}

