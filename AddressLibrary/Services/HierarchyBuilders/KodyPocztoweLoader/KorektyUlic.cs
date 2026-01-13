using System.Collections.Generic;
using System.Linq;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    internal class KorektyUlic
    {
        private class Korekta
        {
            public string UlicaPNA { get; set; } = string.Empty;
            public string Miejscowosc { get; set; } = string.Empty;
            public string Kod { get; set; } = string.Empty;
            public string UlicaPoprawiona { get; set; } = string.Empty;
            public string MiejscowoscPoprawiona { get; set; } = string.Empty;
        }

        private static readonly List<Korekta> _korekty = new()
        {
            //???
            new Korekta
            {
                UlicaPNA = "ArcheologÛw", Miejscowosc = "Warszawa", Kod = "02-184",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //Zlikwidowana W Dzienniku UrzÍdowym WojewÛdztwa ålπskiego z dnia 5 marca 2018 r. ukaza≥a siÍ ww. uchwa≥a (Dz. Urz. Woj. ålπskiego poz. 1398)
            new Korekta
            {
                UlicaPNA = "Arki Boøka", Miejscowosc = "Ruda ålπska", Kod = "41-711",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Borsucza", Miejscowosc = "£Ûdü", Kod = "92-327",
                UlicaPoprawiona = "Kraszewskiego", MiejscowoscPoprawiona = ""
            },
            //Nie ma takiej ulicy w Warszawie, to b≥πd PNA
            new Korekta
            {
                UlicaPNA = "BudziszyÒska", Miejscowosc = "Warszawa", Kod = "01-261",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //Podobnie jak ulice Ciskaczy (Soúnica), Obrotycka (rejon PszczyÒskiej) i RÍbaczy (rejon Sztygarskiej), ktÛre zosta≥y utworzone w wyniku przemianowania niemieckich nazw, a obecnie rÛwnieø trudno je zidentyfikowaÊ w terenie.
            new Korekta
            {
                UlicaPNA = "Ciskaczy", Miejscowosc = "Gliwice", Kod = "44-103",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Czarna", Miejscowosc = "£Ûdü", Kod = "91-306",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //W Poznaniu ulica Duszna, po≥oøona na £azarzu, historycznie istnia≥a od 1955 roku, ale zosta≥a wyburzona w zwiπzku z budowπ centrum handlowego Metropolis, stajπc siÍ czÍúciπ planowanej inwestycji, a jej nazwa zniknÍ≥a z mapy miasta.
            new Korekta
            {
                UlicaPNA = "Duszna", Miejscowosc = "PoznaÒ", Kod = "60-208",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Dzia≥ki ZerzeÒ", Miejscowosc = "Warszawa", Kod = "04-871",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "EkologÛw Skw.", Miejscowosc = "Dar≥owo", Kod = "76-153",
                UlicaPoprawiona = "Skw. EkologÛw", MiejscowoscPoprawiona = ""
            },
            // 2011: Do niedawna istnia≥a uliczka Ewarysta Backiego, przy budowie mostu pÛ≥nocnego zosta≥a
            // calkowicie zlikwidowana, choÊ nie by≥o przy niej juz od dawna zadnych bydynkÛw
            // ot taki kawa≥ek asfaltowej drogi wsrÛd traw.
            new Korekta
            {
                UlicaPNA = "Ewarysta Bronis≥awa Backiego", Miejscowosc = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Franciszka Øwirki i Stanis≥awa Wigury", Miejscowosc = "BÍdzin", Kod = "42-500",
                UlicaPoprawiona = "Øwirki i Wigury", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Franciszka Øwirki i Stanis≥awa Wigury", Miejscowosc = "BieruÒ", Kod = "43-150",
                UlicaPoprawiona = "Øwirki i Wigury", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Henryka Sztompki", Miejscowosc = "Lublin", Kod = "20-862",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "I Dywizji Al.", Miejscowosc = "£Ûdü", Kod = "91-836",
                UlicaPoprawiona = "Al. Pierwszej Dywizji", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Ilustracji", Miejscowosc = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Julija Beneszicia", Miejscowosc = "Warszawa", Kod = "03-127",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Karola Krejczego", Miejscowosc = "Warszawa", Kod = "03-127",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Klechdy", Miejscowosc = "Warszawa", Kod = "03-782",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Kolejowa", Miejscowosc = "Zabrze", Kod = "41-800",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Lelum", Miejscowosc = "Warszawa", Kod = "01-920",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Leona Jana Landowskiego", Miejscowosc = "Warszawa", Kod = "03-720",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Leona Rodala", Miejscowosc = "Warszawa", Kod = "00-215",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Ma≥ego Franka", Miejscowosc = "Warszawa", Kod = "01-115",
                UlicaPoprawiona = "SiedzikÛwny \"Inki\"", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Ma≥ego Franka", Miejscowosc = "Warszawa", Kod = "01-449",
                UlicaPoprawiona = "SiedzikÛwny \"Inki\"", MiejscowoscPoprawiona = ""
            },
            //???
                        new Korekta
            {
                UlicaPNA = "Melchiora WaÒkowicza", Miejscowosc = "£Ûdü", Kod = "93-636",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Mestwina", Miejscowosc = "Warszawa", Kod = "03-175",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Na Prze≥Íczy", Miejscowosc = "Lublin", Kod = "20-564",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Niedüwiedzia", Miejscowosc = "£Ûdü", Kod = "92-323",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "ONZ Rondo", Miejscowosc = "Warszawa", Kod = "00-124",
                UlicaPoprawiona = "Rondo Organizacji NarodÛw Zjednoczonych", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "OúwiÍcimska", Miejscowosc = "£Ûdü", Kod = "93-542",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Pa≥πk", Miejscowosc = "Warszawa", Kod = "02-268",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Pantomimy", Miejscowosc = "Warszawa", Kod = "01-979",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "PCK Al.", Miejscowosc = "£Ûdü", Kod = "90-456",
                UlicaPoprawiona = "Aleja Polskiego Czerwonego Krzyøa", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piechoty Wybranieckiej", Miejscowosc = "£Ûdü", Kod = "92-438",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Pi≥sudskiego Fort", Miejscowosc = "Warszawa", Kod = "02-704",
                UlicaPoprawiona = "Fort Pi≥sudskiego", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piotra Kartina", Miejscowosc = "Warszawa", Kod = "03-597",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Piotra Skuratowicza", Miejscowosc = "Warszawa", Kod = "03-982",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Podwale", Miejscowosc = "£Ûdü", Kod = "93-430",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Poleg≥ych", Miejscowosc = "Warszawa", Kod = "01-979",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Polelum", Miejscowosc = "Warszawa", Kod = "01-920",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Przystaniowa", Miejscowosc = "Warszawa", Kod = "00-408",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //Podobnie jak ulice Ciskaczy (Soúnica), Obrotycka (rejon PszczyÒskiej) i RÍbaczy (rejon Sztygarskiej), ktÛre zosta≥y utworzone w wyniku przemianowania niemieckich nazw, a obecnie rÛwnieø trudno je zidentyfikowaÊ w terenie.
                        new Korekta
            {
                UlicaPNA = "RÍbaczy", Miejscowosc = "Gliwice", Kod = "44-103",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "Rycerski Pl.", Miejscowosc = "£Ûdü", Kod = "92-441",
                UlicaPoprawiona = "Pl. Rycerski", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Rytmy", Miejscowosc = "Warszawa", Kod = "01-966",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Serpentyna", Miejscowosc = "£Ûdü", Kod = "92-005",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Spawalnicza", Miejscowosc = "Warszawa", Kod = "03-869",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Szpitalna", Miejscowosc = "Lublin", Kod = "20-708",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "TrubadurÛw", Miejscowosc = "Warszawa", Kod = "02-859",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wajdeloty", Miejscowosc = "Warszawa", Kod = "01-916",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Welwetowa", Miejscowosc = "Warszawa", Kod = "02-833",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wiktora Pilicha", Miejscowosc = "Zabrze", Kod = "41-800",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Wypoczynkowa", Miejscowosc = "Warszawa", Kod = "03-017",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "ZajÍcza", Miejscowosc = "Konin", Kod = "62-510",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            //???
            new Korekta
            {
                UlicaPNA = "Zgrzebna", Miejscowosc = "Warszawa", Kod = "03-869",
                UlicaPoprawiona = "", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "ZHP Al.", Miejscowosc = "£Ûdü", Kod = "90-440",
                UlicaPoprawiona = "Zwiπzku Harcerstwa Polskiego", MiejscowoscPoprawiona = ""
            },
            new Korekta
            {
                UlicaPNA = "èrÛdliska I (Palmiarnia) Park", Miejscowosc = "£Ûdü", Kod = "90-329",
                UlicaPoprawiona = "Park èrÛdliska", MiejscowoscPoprawiona = ""
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

        public static string Popraw(string ulica, string miejscowosc, string kod)
        {
            //if (ulica.Contains("PCK"))
            //{
            //    var yy = 1;
            //}
            var korekta = _korekty.FirstOrDefault(k =>
                k.UlicaPNA == ulica &&
                k.Miejscowosc == miejscowosc &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.UlicaPoprawiona))
            {
                // Normalizuj poprawionπ nazwÍ - usuÒ prefiks jeúli jest
                // return NormalizujNazweUlicy(korekta.UlicaPoprawiona);
                 return korekta.UlicaPoprawiona;
            }

            return ulica;
        }

        public static string PoprawMiejscowosc(string ulica, string miejscowosc, string kod)
        {
            var korekta = _korekty.FirstOrDefault(k =>
                k.UlicaPNA == ulica &&
                k.Miejscowosc == miejscowosc &&
                k.Kod == kod);

            if (korekta != null && !string.IsNullOrEmpty(korekta.MiejscowoscPoprawiona))
            {
                return korekta.MiejscowoscPoprawiona;
            }

            return miejscowosc;
        }
    }
}