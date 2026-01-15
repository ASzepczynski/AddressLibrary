using System.Collections.Generic;
using System.Linq;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    internal class KorektyUlic
    {
        private class Korekta
        {
            public string UlicaPNA { get; set; } = string.Empty;
            public string Miasto { get; set; } = string.Empty;
            public string Kod { get; set; } = string.Empty;
            public string UlicaPoprawiona { get; set; } = string.Empty;
            public string MiastoPoprawiona { get; set; } = string.Empty;
        }

        private static readonly List<Korekta> _korekty = new()
        {
            //???
            new Korekta
            {
                UlicaPNA = "ArcheologÛw", Miasto = "Warszawa", Kod = "02-184",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //Zlikwidowana W Dzienniku UrzÍdowym WojewÛdztwa ålπskiego z dnia 5 marca 2018 r. ukaza≥a siÍ ww. uchwa≥a (Dz. Urz. Woj. ålπskiego poz. 1398)
            new Korekta
            {
                UlicaPNA = "Arki Boøka", Miasto = "Ruda ålπska", Kod = "41-711",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Borsucza", Miasto = "£Ûdü", Kod = "92-327",
                UlicaPoprawiona = "Kraszewskiego", MiastoPoprawiona = ""
            },
            //Nie ma takiej ulicy w Warszawie, to b≥πd PNA
            new Korekta
            {
                UlicaPNA = "BudziszyÒska", Miasto = "Warszawa", Kod = "01-261",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //Podobnie jak ulice Ciskaczy (Soúnica), Obrotycka (rejon PszczyÒskiej) i RÍbaczy (rejon Sztygarskiej), ktÛre zosta≥y utworzone w wyniku przemianowania niemieckich nazw, a obecnie rÛwnieø trudno je zidentyfikowaÊ w terenie.
            new Korekta
            {
                UlicaPNA = "Ciskaczy", Miasto = "Gliwice", Kod = "44-103",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Czarna", Miasto = "£Ûdü", Kod = "91-306",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //W Poznaniu ulica Duszna, po≥oøona na £azarzu, historycznie istnia≥a od 1955 roku, ale zosta≥a wyburzona w zwiπzku z budowπ centrum handlowego Metropolis, stajπc siÍ czÍúciπ planowanej inwestycji, a jej nazwa zniknÍ≥a z mapy miasta.
            new Korekta
            {
                UlicaPNA = "Duszna", Miasto = "PoznaÒ", Kod = "60-208",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Dzia≥ki ZerzeÒ", Miasto = "Warszawa", Kod = "04-871",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "EkologÛw Skw.", Miasto = "Dar≥owo", Kod = "76-153",
                UlicaPoprawiona = "Skw. EkologÛw", MiastoPoprawiona = ""
            },
            // 2011: Do niedawna istnia≥a uliczka Ewarysta Backiego, przy budowie mostu pÛ≥nocnego zosta≥a
            // calkowicie zlikwidowana, choÊ nie by≥o przy niej juz od dawna zadnych bydynkÛw
            // ot taki kawa≥ek asfaltowej drogi wsrÛd traw.
            new Korekta
            {
                UlicaPNA = "Ewarysta Bronis≥awa Backiego", Miasto = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Franciszka Øwirki i Stanis≥awa Wigury", Miasto = "BÍdzin", Kod = "42-500",
                UlicaPoprawiona = "Øwirki i Wigury", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Franciszka Øwirki i Stanis≥awa Wigury", Miasto = "BieruÒ", Kod = "43-150",
                UlicaPoprawiona = "Øwirki i Wigury", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Henryka Sztompki", Miasto = "Lublin", Kod = "20-862",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "I Dywizji Al.", Miasto = "£Ûdü", Kod = "91-836",
                UlicaPoprawiona = "Al. Pierwszej Dywizji", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Ilustracji", Miasto = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Julija Beneszicia", Miasto = "Warszawa", Kod = "03-127",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Karola Krejczego", Miasto = "Warszawa", Kod = "03-127",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Klechdy", Miasto = "Warszawa", Kod = "03-782",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Kolejowa", Miasto = "Zabrze", Kod = "41-800",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Lelum", Miasto = "Warszawa", Kod = "01-920",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Leona Jana Landowskiego", Miasto = "Warszawa", Kod = "03-720",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Leona Rodala", Miasto = "Warszawa", Kod = "00-215",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Ma≥ego Franka", Miasto = "Warszawa", Kod = "01-115",
                UlicaPoprawiona = "SiedzikÛwny \"Inki\"", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Ma≥ego Franka", Miasto = "Warszawa", Kod = "01-449",
                UlicaPoprawiona = "SiedzikÛwny \"Inki\"", MiastoPoprawiona = ""
            },
            //???
                        new Korekta
            {
                UlicaPNA = "Melchiora WaÒkowicza", Miasto = "£Ûdü", Kod = "93-636",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Mestwina", Miasto = "Warszawa", Kod = "03-175",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Na Prze≥Íczy", Miasto = "Lublin", Kod = "20-564",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Niedüwiedzia", Miasto = "£Ûdü", Kod = "92-323",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "ONZ Rondo", Miasto = "Warszawa", Kod = "00-124",
                UlicaPoprawiona = "Rondo Organizacji NarodÛw Zjednoczonych", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "OúwiÍcimska", Miasto = "£Ûdü", Kod = "93-542",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Pa≥πk", Miasto = "Warszawa", Kod = "02-268",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Pantomimy", Miasto = "Warszawa", Kod = "01-979",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "PCK Al.", Miasto = "£Ûdü", Kod = "90-456",
                UlicaPoprawiona = "Aleja Polskiego Czerwonego Krzyøa", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piechoty Wybranieckiej", Miasto = "£Ûdü", Kod = "92-438",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Pi≥sudskiego Fort", Miasto = "Warszawa", Kod = "02-704",
                UlicaPoprawiona = "Fort Pi≥sudskiego", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piotra Kartina", Miasto = "Warszawa", Kod = "03-597",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piotra Skuratowicza", Miasto = "Warszawa", Kod = "03-982",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Podwale", Miasto = "£Ûdü", Kod = "93-430",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Poleg≥ych", Miasto = "Warszawa", Kod = "01-979",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Polelum", Miasto = "Warszawa", Kod = "01-920",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Przystaniowa", Miasto = "Warszawa", Kod = "00-408",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //Podobnie jak ulice Ciskaczy (Soúnica), Obrotycka (rejon PszczyÒskiej) i RÍbaczy (rejon Sztygarskiej), ktÛre zosta≥y utworzone w wyniku przemianowania niemieckich nazw, a obecnie rÛwnieø trudno je zidentyfikowaÊ w terenie.
                        new Korekta
            {
                UlicaPNA = "RÍbaczy", Miasto = "Gliwice", Kod = "44-103",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Rycerski Pl.", Miasto = "£Ûdü", Kod = "92-441",
                UlicaPoprawiona = "Pl. Rycerski", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Rytmy", Miasto = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Serpentyna", Miasto = "£Ûdü", Kod = "92-005",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Spawalnicza", Miasto = "Warszawa", Kod = "03-869",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Szpitalna", Miasto = "Lublin", Kod = "20-708",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "TrubadurÛw", Miasto = "Warszawa", Kod = "02-859",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wajdeloty", Miasto = "Warszawa", Kod = "01-916",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Welwetowa", Miasto = "Warszawa", Kod = "02-833",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wiktora Pilicha", Miasto = "Zabrze", Kod = "41-800",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wypoczynkowa", Miasto = "Warszawa", Kod = "03-017",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "ZajÍcza", Miasto = "Konin", Kod = "62-510",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Zgrzebna", Miasto = "Warszawa", Kod = "03-869",
                UlicaPoprawiona = "", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "ZHP Al.", Miasto = "£Ûdü", Kod = "90-440",
                UlicaPoprawiona = "Zwiπzku Harcerstwa Polskiego", MiastoPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "èrÛdliska I (Palmiarnia) Park", Miasto = "£Ûdü", Kod = "90-329",
                UlicaPoprawiona = "Park èrÛdliska", MiastoPoprawiona = ""
            }
        };

        /// <summary>
        /// Normalizuje nazwÍ ulicy przez zamianÍ kolejnoúci prefiksu i nazwy.
        /// Np. "Al. PCK" -> "PCK", "Skw. EkologÛw" -> "EkologÛw"
        /// W bazie ulice sπ przechowywane jako Nazwa1 (g≥Ûwna czÍúÊ) + Nazwa2 (prefix/cecha).
        /// </summary>
        private static string NormalizujNazweUlicy(string nazwa)
        {
            if (string.IsNullOrWhiteSpace(nazwa))
                return nazwa;

            // Lista prefiksÛw do sprawdzenia (z kropkπ i spacjπ)
            string[] prefiksy = {
                "al. ", "pl. ", "os. ", "rondo ", "park ", "skwer ", "bulw. ",
                "rynek ", "wyb. ", "wyspa ", "droga ", "ogrÛd ", "skw. "
            };

            var nazwaLower = nazwa.ToLowerInvariant();

            foreach (var prefix in prefiksy)
            {
                if (nazwaLower.StartsWith(prefix))
                {
                    // UsuÒ prefix i zwrÛÊ tylko nazwÍ g≥Ûwnπ
                    return nazwa.Substring(prefix.Length).Trim();
                }
            }

            return nazwa;
        }

        public static string Popraw(string ulica, string miasto, string kod)
        {
            //if (ulica.Contains("PCK"))
            //{
            //    var yy = 1;
            //}
            var korekta = _korekty.FirstOrDefault(k =>
                k.UlicaPNA == ulica &&
                k.Miasto == miasto &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.UlicaPoprawiona))
            {
                // Normalizuj poprawionπ nazwÍ - usuÒ prefiks jeúli jest
                // return NormalizujNazweUlicy(korekta.UlicaPoprawiona);
                 return korekta.UlicaPoprawiona;
            }

            return ulica;
        }

        public static string PoprawMiasto(string ulica, string miasto, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.UlicaPNA == ulica &&
                k.Miasto == miasto &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.MiastoPoprawiona))
            {
                return korekta.MiastoPoprawiona;
            }

            return miasto;
        }
    }
}