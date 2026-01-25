using AddressLibrary.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
