// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Walidator numerów budynków z obsługą zakresów
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

            if (zakres.Contains('-'))
            {
                var czesci = zakres.Split('-', StringSplitOptions.TrimEntries);
                if (czesci.Length != 2)
                {
                    return false;
                }

                var poczatek = czesci[0];
                var koniec = czesci[1];

                // Wyciągnij liczbę z początku (obsługa 52a, 115b itp.)
                if (!ExtractNumber(poczatek, out int numerPoczatek))
                {
                    return false;
                }

                if (koniec.Equals("DK", StringComparison.OrdinalIgnoreCase))
                {
                    // ✅ NOWA LOGIKA: Jeśli zakres bez (n)/(p), sprawdź parzystość
                    if (!tylkoNieparzyste && !tylkoParzyste)
                    {
                        tylkoParzyste = numerPoczatek % 2 == 0;
                        tylkoNieparzyste = !tylkoParzyste;
                    }

                    // Sprawdź parzystość
                    if (tylkoNieparzyste && czyParzysty)
                    {
                        return false;
                    }
                    if (tylkoParzyste && !czyParzysty)
                    {
                        return false;
                    }

                    return numer >= numerPoczatek;
                }

                // Wyciągnij liczbę z końca (obsługa 52a, 115b itp.)
                if (!ExtractNumber(koniec, out int numerKoniec))
                {
                    return false;
                }

                // ✅ NOWA LOGIKA: Jeśli zakres bez (n)/(p) ma jednakową parzystość na początku i końcu
                if (!tylkoNieparzyste && !tylkoParzyste && numerPoczatek % 2 == numerKoniec % 2)
                {
                    tylkoParzyste = numerPoczatek % 2 == 0;
                    tylkoNieparzyste = !tylkoParzyste;
                }

                // Sprawdź parzystość
                if (tylkoNieparzyste && czyParzysty)
                {
                    return false;
                }
                if (tylkoParzyste && !czyParzysty)
                {
                    return false;
                }

                return numer >= numerPoczatek && numer <= numerKoniec;
            }

            // ✅ NOWA LOGIKA: Pojedynczy numer bez (n)/(p) dziedziczy parzystość z wartości
            if (ExtractNumber(zakres, out int pojedynczyNumer))
            {
                // Jeśli nie ma jawnego oznaczenia (n)/(p), ustal parzystość na podstawie liczby
                if (!tylkoNieparzyste && !tylkoParzyste)
                {
                    tylkoParzyste = pojedynczyNumer % 2 == 0;
                    tylkoNieparzyste = !tylkoParzyste;
                }

                // Sprawdź zgodność numeru
                if (numer != pojedynczyNumer)
                {
                    return false;
                }

                // Sprawdź parzystość
                if (tylkoNieparzyste && czyParzysty)
                {
                    return false;
                }
                if (tylkoParzyste && !czyParzysty)
                {
                    return false;
                }

                return true;
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