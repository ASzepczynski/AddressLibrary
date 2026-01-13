// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Walidator numerów budynków z obs³ug¹ zakresów
    /// </summary>
    public class BuildingNumberValidator
    {
        /// <summary>
        /// Sprawdza czy numer budynku pasuje do definicji zakresu
        /// </summary>
        public bool IsNumberInRange(string numerBudynku, string definicjaZakresow)
        {
            if (string.IsNullOrWhiteSpace(definicjaZakresow))
            {
                return true;
            }

            if (!ExtractNumber(numerBudynku, out int numer))
            {
                return false;
            }

            var zakresy = definicjaZakresow.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var zakres in zakresy)
            {
                if (IsNumberInSingleRange(numer, zakres))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNumberInSingleRange(int numer, string zakres)
        {
            zakres = zakres.Trim();

            bool tylkoNieparzyste = zakres.EndsWith("(n)", StringComparison.OrdinalIgnoreCase);
            bool tylkoParzyste = zakres.EndsWith("(p)", StringComparison.OrdinalIgnoreCase);

            if (tylkoNieparzyste || tylkoParzyste)
            {
                zakres = zakres.Substring(0, zakres.Length - 3).Trim();
            }

            bool czyParzysty = numer % 2 == 0;
            if (tylkoNieparzyste && czyParzysty)
            {
                return false;
            }
            if (tylkoParzyste && !czyParzysty)
            {
                return false;
            }

            if (zakres.Contains('-'))
            {
                var czesci = zakres.Split('-', StringSplitOptions.TrimEntries);
                if (czesci.Length != 2)
                {
                    return false;
                }

                var poczatek = czesci[0];
                var koniec = czesci[1];

                if (!int.TryParse(poczatek, out int numerPoczatek))
                {
                    return false;
                }

                if (koniec.Equals("DK", StringComparison.OrdinalIgnoreCase))
                {
                    return numer >= numerPoczatek;
                }

                if (!int.TryParse(koniec, out int numerKoniec))
                {
                    return false;
                }

                return numer >= numerPoczatek && numer <= numerKoniec;
            }

            if (int.TryParse(zakres, out int pojedynczyNumer))
            {
                return numer == pojedynczyNumer;
            }

            return false;
        }

        private bool ExtractNumber(string numerBudynku, out int numer)
        {
            numer = 0;

            if (string.IsNullOrWhiteSpace(numerBudynku))
            {
                return false;
            }

            numerBudynku = numerBudynku.Trim();

            if (numerBudynku.Contains('/'))
            {
                numerBudynku = numerBudynku.Split('/')[0].Trim();
            }

            var cyfry = new StringBuilder();
            foreach (char c in numerBudynku)
            {
                if (char.IsDigit(c))
                {
                    cyfry.Append(c);
                }
                else
                {
                    break;
                }
            }

            if (cyfry.Length > 0)
            {
                return int.TryParse(cyfry.ToString(), out numer);
            }

            return false;
        }
    }
}