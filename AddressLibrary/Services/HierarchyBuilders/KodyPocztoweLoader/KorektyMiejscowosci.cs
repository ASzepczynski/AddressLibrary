using System.Collections.Generic;
using System.Linq;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    internal class KorektyMiejscowosci
    {
        public class Korekta
        {
            public string MiejscowoscPNA { get; set; } = string.Empty;
            public string Gmina { get; set; } = string.Empty;
            public string Powiat { get; set; } = string.Empty;
            public string Wojewodztwo { get; set; } = string.Empty;
            public string Kod { get; set; } = string.Empty;
            public string MiejscowoscPoprawiona { get; set; } = string.Empty;
            public string GminaPoprawiona { get; set; } = string.Empty;
        }

        private static readonly List<Korekta> _korekty = new()
        {
            new Korekta
            {
                MiejscowoscPNA = "Bagno", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do S³upska
            new Korekta
            {
                MiejscowoscPNA = "Boles³awice", Gmina = "Kobylnica", Powiat = "", Wojewodztwo = "", Kod = "76-251",
                MiejscowoscPoprawiona = "S³upsk", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "Bratian", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Chroœle", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // Kolonia. Zniesiona i w³¹czona do Adamki
            new Korekta
            {
                MiejscowoscPNA = "Grabinka", Gmina = "Zadzim", Powiat = "", Wojewodztwo = "", Kod = "99-232",
                MiejscowoscPoprawiona = "Adamka", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "GryŸliny", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "GwiŸdziny", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Jamielnik", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Kaczek", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Lekarty", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "£¹ki Bratiañskie", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "\"Mazewo Dworskie\"\"A\"\"\"", Gmina = "Nasielsk", Powiat = "", Wojewodztwo = "", Kod = "05-190",
                MiejscowoscPoprawiona = "Mazewo Dworskie\"A\"", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "\"Mazewo Dworskie\"\"B\"\"\"", Gmina = "Nasielsk", Powiat = "", Wojewodztwo = "", Kod = "05-190",
                MiejscowoscPoprawiona = "Mazewo Dworskie\"B\"", GminaPoprawiona = ""
            },
            // 1 stycznia 2026 osadê Milejów-Osada zniesiono
            new Korekta
            {
                MiejscowoscPNA = "Milejów-Osada", Gmina = "Milejów", Powiat = "", Wojewodztwo = "", Kod = "21-020",
                MiejscowoscPoprawiona = "Milejów", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "Mszanowo", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Nawra", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Nowy Dwór Bratiañski", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Pacó³towo", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Pustki", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Radomno", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Repetajka", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // do 2025 Ró¿aniec, teraz Ró¿aniec Pierwszy i Ró¿aniec Drugi
            new Korekta
            {
                MiejscowoscPNA = "Ró¿aniec", Gmina = "Tarnogród", Powiat = "", Wojewodztwo = "", Kod = "23-420",
                MiejscowoscPoprawiona = "Ró¿aniec Pierwszy", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "Ruda", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do Lêborka
            new Korekta
            {
                MiejscowoscPNA = "Rybki", Gmina = "Nowa Wieœ Lêborska", Powiat = "", Wojewodztwo = "", Kod = "84-315",
                MiejscowoscPoprawiona = "Lêbork", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "Skarlin", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // do 2025 Stary Radziejów
            new Korekta
            {
                MiejscowoscPNA = "Stary Radziejów", Gmina = "Radziejów", Powiat = "", Wojewodztwo = "", Kod = "88-200",
                MiejscowoscPoprawiona = "Stary Radziejów-Kolonia", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiejscowoscPNA = "Studa", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiejscowoscPNA = "Tylice", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiejscowoscPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do Ma³kini Górnej 
            new Korekta
            {
                MiejscowoscPNA = "Zawisty Dzikie", Gmina = "Ma³kinia Górna", Powiat = "", Wojewodztwo = "", Kod = "07-320",
                MiejscowoscPoprawiona = "Ma³kinia Górna", GminaPoprawiona = ""
            }
        };

        /// <summary>
        /// Znajduje korektê dla danej miejscowoœci
        /// </summary>
        public static Korekta? Znajdz(string miejscowosc, string gmina, string kod)
        {
            return _korekty.FirstOrDefault(k =>
                k.MiejscowoscPNA == miejscowosc &&
                k.Gmina == gmina &&
                k.Kod == kod);
        }

        /// <summary>
        /// Zwraca poprawion¹ nazwê miejscowoœci lub oryginaln¹ jeœli nie ma korekty
        /// </summary>
        public static string Popraw(string miejscowosc, string gmina, string powiat, string wojewodztwo, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.MiejscowoscPNA == miejscowosc &&
                k.Gmina == gmina &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.MiejscowoscPoprawiona))
            {
                return korekta.MiejscowoscPoprawiona;
            }

            return miejscowosc;
        }

        /// <summary>
        /// Zwraca poprawion¹ nazwê gminy lub oryginaln¹ jeœli nie ma korekty
        /// </summary>
        public static string PoprawGmina(string miejscowosc, string gmina, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.MiejscowoscPNA == miejscowosc &&
                k.Gmina == gmina &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.GminaPoprawiona))
            {
                return korekta.GminaPoprawiona;
            }

            return gmina;
        }
    }
}