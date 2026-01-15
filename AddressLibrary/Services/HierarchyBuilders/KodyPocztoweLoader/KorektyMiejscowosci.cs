using System.Collections.Generic;
using System.Linq;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    internal class KorektyMiasta
    {
        public class Korekta
        {
            public string MiastoPNA { get; set; } = string.Empty;
            public string Gmina { get; set; } = string.Empty;
            public string Powiat { get; set; } = string.Empty;
            public string Wojewodztwo { get; set; } = string.Empty;
            public string Kod { get; set; } = string.Empty;
            public string MiastoPoprawiona { get; set; } = string.Empty;
            public string GminaPoprawiona { get; set; } = string.Empty;
        }

        private static readonly List<Korekta> _korekty = new()
        {
            new Korekta
            {
                MiastoPNA = "Bagno", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do S³upska
            new Korekta
            {
                MiastoPNA = "Boles³awice", Gmina = "Kobylnica", Powiat = "", Wojewodztwo = "", Kod = "76-251",
                MiastoPoprawiona = "S³upsk", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "Bratian", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Chroœle", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // Kolonia. Zniesiona i w³¹czona do Adamki
            new Korekta
            {
                MiastoPNA = "Grabinka", Gmina = "Zadzim", Powiat = "", Wojewodztwo = "", Kod = "99-232",
                MiastoPoprawiona = "Adamka", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "GryŸliny", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "GwiŸdziny", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Jamielnik", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Kaczek", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Lekarty", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "£¹ki Bratiañskie", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "\"Mazewo Dworskie\"\"A\"\"\"", Gmina = "Nasielsk", Powiat = "", Wojewodztwo = "", Kod = "05-190",
                MiastoPoprawiona = "Mazewo Dworskie\"A\"", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "\"Mazewo Dworskie\"\"B\"\"\"", Gmina = "Nasielsk", Powiat = "", Wojewodztwo = "", Kod = "05-190",
                MiastoPoprawiona = "Mazewo Dworskie\"B\"", GminaPoprawiona = ""
            },
            // 1 stycznia 2026 osadê Milejów-Osada zniesiono
            new Korekta
            {
                MiastoPNA = "Milejów-Osada", Gmina = "Milejów", Powiat = "", Wojewodztwo = "", Kod = "21-020",
                MiastoPoprawiona = "Milejów", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "Mszanowo", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Nawra", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Nowy Dwór Bratiañski", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Pacó³towo", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Pustki", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Radomno", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Repetajka", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // do 2025 Ró¿aniec, teraz Ró¿aniec Pierwszy i Ró¿aniec Drugi
            new Korekta
            {
                MiastoPNA = "Ró¿aniec", Gmina = "Tarnogród", Powiat = "", Wojewodztwo = "", Kod = "23-420",
                MiastoPoprawiona = "Ró¿aniec Pierwszy", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "Ruda", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-304",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do Lêborka
            new Korekta
            {
                MiastoPNA = "Rybki", Gmina = "Nowa Wieœ Lêborska", Powiat = "", Wojewodztwo = "", Kod = "84-315",
                MiastoPoprawiona = "Lêbork", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "Skarlin", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // do 2025 Stary Radziejów
            new Korekta
            {
                MiastoPNA = "Stary Radziejów", Gmina = "Radziejów", Powiat = "", Wojewodztwo = "", Kod = "88-200",
                MiastoPoprawiona = "Stary Radziejów-Kolonia", GminaPoprawiona = ""
            },
            new Korekta
            {
                MiastoPNA = "Studa", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-332",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            new Korekta
            {
                MiastoPNA = "Tylice", Gmina = "Nowe Miasto Lubawskie", Powiat = "", Wojewodztwo = "", Kod = "13-300",
                MiastoPoprawiona = "", GminaPoprawiona = "Bratian"
            },
            // W³¹czone terytorialnie do Ma³kini Górnej 
            new Korekta
            {
                MiastoPNA = "Zawisty Dzikie", Gmina = "Ma³kinia Górna", Powiat = "", Wojewodztwo = "", Kod = "07-320",
                MiastoPoprawiona = "Ma³kinia Górna", GminaPoprawiona = ""
            }
        };

        /// <summary>
        /// Znajduje korektê dla danej miejscowoœci
        /// </summary>
        public static Korekta? Znajdz(string miasto, string gmina, string kod)
        {
            return _korekty.FirstOrDefault(k =>
                k.MiastoPNA == miasto &&
                k.Gmina == gmina &&
                k.Kod == kod);
        }

        /// <summary>
        /// Zwraca poprawion¹ nazwê miejscowoœci lub oryginaln¹ jeœli nie ma korekty
        /// </summary>
        public static string Popraw(string miasto, string gmina, string powiat, string wojewodztwo, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.MiastoPNA == miasto &&
                k.Gmina == gmina &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.MiastoPoprawiona))
            {
                return korekta.MiastoPoprawiona;
            }

            return miasto;
        }

        /// <summary>
        /// Zwraca poprawion¹ nazwê gminy lub oryginaln¹ jeœli nie ma korekty
        /// </summary>
        public static string PoprawGmina(string miasto, string gmina, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.MiastoPNA == miasto &&
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