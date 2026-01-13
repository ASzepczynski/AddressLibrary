using System.Text.RegularExpressions;
using System.Collections.Generic;
using AddressLibrary.Models;

public static partial class PdfProcessor
{
    public static bool WyprowadzDane(List<string> slowa, List<Pna> records, string Delimiter)
    {
        var woj = new List<string>()
        {
            "ma≥opolskie",
            "úlπskie",
            "≥Ûdzkie",
            "dolnoúlπskie",
            "opolskie",
            "kujawsko-pomorskie",
            "warmiÒsko-mazurskie",
            "podlaskie",
            "pomorskie",
            "zachodniopomorskie",
            "lubuskie",
            "wielkopolskie",
            "lubelskie",
            "podkarpackie",
            "úwiÍtokrzyskie",
            "mazowieckie"
        };

        if (slowa == null || slowa.Count == 0) return false;

        var numeryList = new List<string>();
        var daneList = new List<string>();

        for (int index = 0; index < slowa.Count; index++)
        {
            var field = slowa[index];

            if (field != null && (field.Contains("©") || field.IndexOf("copyright", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                // stop outputting further fields when a copyright marker is encountered
                break;
            }

            if (field == null) continue;

            // fast check: first non-space char must be a digit
            int idx = 0;
            while (idx < field.Length && char.IsWhiteSpace(field[idx])) idx++;

            // Do not treat the first element in 'slowa' as a number token
            bool isFirstElement = index == 0;

            if (!isFirstElement && idx < field.Length && char.IsDigit(field[idx]))
            {
                if (IsNumberToken(field.AsSpan(), out var cleaned))
                {
                    numeryList.Add(cleaned);
                    continue; // don't print this field
                }

                // fallback: if token ends with a comma and before comma it's digits + optional letters, treat as number
                var trimmed = field.Trim();
                if (trimmed.EndsWith(","))
                {
                    var withoutComma = trimmed.Substring(0, trimmed.Length - 1).Trim();
                    if (withoutComma.Length > 0 && char.IsDigit(withoutComma[0]))
                    {
                        bool allOk = true;
                        for (int k = 0; k < withoutComma.Length; k++)
                        {
                            if (!(char.IsDigit(withoutComma[k]) || char.IsLetter(withoutComma[k]))) { allOk = false; break; }
                        }
                        if (allOk)
                        {
                            numeryList.Add(withoutComma);
                            continue;
                        }
                    }
                }
            }

            daneList.Add(field);

        }

        var daneArr = daneList.ToArray();
        if (daneList.Contains("ArchitektÛw"))
        {
            int y = 1;
        }

        int nDelta = 0;
        string sUlica = string.Empty;
        string sResztaMiasto = string.Empty;
        string sResztaUlica = string.Empty;

        if (daneArr.Count() < 4)
        {
            int vv = 1;
        }

        if (daneArr[3] == "kÍdzierzyÒsko-")
        {
            daneArr[3] = "kÍdzierzyÒsko-kozielski";
            daneArr[5] = daneArr[5].Replace(daneArr[5], "kozielski");
            if (daneArr[5] == "")
            {
                daneArr = daneArr.Take(daneArr.Length - 1).ToArray();
            }
        }
        if (daneArr[2] == "Nowy DwÛr Mazowiecki" && daneArr[0].EndsWith("Nowy DwÛr") && daneArr[5] == "Mazowiecki")
        {
            daneArr[0] += " " + daneArr[5];
            daneArr = daneArr.Take(daneArr.Length - 1).ToArray();
            sUlica = daneArr[1];
            nDelta = 0;
        }

    

        if (daneArr[0].EndsWith("Czerwionka-") && daneArr.Count()==6)
        {
            daneArr[0] += daneArr[5];
            daneArr = daneArr.Take(daneArr.Length - 1).ToArray();
            sUlica = daneArr[1];
            nDelta = 1;
            goto dalej;
        }

        if (daneArr.Count() >= 5)
        {
           for (int i = 5; i < daneArr.Count(); i++)
            {
               if (daneArr[i].Contains(")"))
                  {
                    sResztaMiasto += daneArr[i].Trim();
                    }
                    else
                    {
                        if (!woj.Contains(daneArr[i].ToString()))
                        {
                            sResztaUlica += " " + daneArr[i].Trim();
                        }
                    }
                }
                sUlica = daneArr[1] + sResztaUlica;
                nDelta = 1;
            }
    dalej:
        var numery = string.Join(",", numeryList);

        string sKodMiasto = daneArr[0] + sResztaMiasto;
        string sDzielnica = string.Empty;
        string sGmina = string.Empty;
        string sPowiat = string.Empty;
        string sWojewodztwo = string.Empty;

        if (daneArr.Count() > 3)
        {
            sGmina = daneArr[1 + nDelta];
            sPowiat = daneArr[2 + nDelta];
            sWojewodztwo = daneArr[3 + nDelta];
        }
        else
        {
            sGmina = "ERROR";
            sPowiat = daneArr[1];
            sWojewodztwo = daneArr[2];
        }

        // Jeúli gmina koÒczy siÍ minusem
        if (sGmina.EndsWith("-"))
        {
            var pow = sGmina + sWojewodztwo;
            sWojewodztwo = sPowiat;
            sGmina = sUlica;
            sUlica = string.Empty;
            sPowiat = pow;
        }

        // Jeúli wojewÛdztwo zawiera znak koÒca nawiasu, usuÒ wszystko od tego znaku w≥πcznie
        int parenIndex = sWojewodztwo.IndexOf(')');
        if (parenIndex != -1)
        {
            sKodMiasto += sWojewodztwo;
            sWojewodztwo = sPowiat;
            sPowiat = sGmina;
            numery = numery.Replace(sGmina, string.Empty).Trim();
        }

        // If sKodMiasto contains text in parentheses, extract it to sDzielnica and remove it from sKodMiasto
        var m = Regex.Match(sKodMiasto, "\\(([^)]*)\\)");
        if (m.Success)
        {
            sDzielnica = m.Groups[1].Value.Trim();
            sKodMiasto = (sKodMiasto.Remove(m.Index, m.Length)).Trim();
        }

        // split sKodMiasto into sKod and sMiasto by first space
        string sKod = sKodMiasto;
        string sMiasto = string.Empty;
        int firstSpace = sKodMiasto.IndexOf(' ');
        if (firstSpace >= 0)
        {
            sKod = sKodMiasto.Substring(0, firstSpace);
            sMiasto = sKodMiasto.Substring(firstSpace + 1).Trim();
        }

        if (sUlica == "èrÛdliska I (Palmiarnia)")
        {
            sUlica = "èrÛdliska I (Palmiarnia) Park";
            sWojewodztwo = "≥Ûdzkie";
            sGmina = "£Ûdü";
            sPowiat = "£Ûdü";
        }

        if (sKodMiasto.StartsWith("00-940"))
        {
            return true;
        }

        // create CP and add to records
        var cp = new Pna
        {
            Kod = sKod,
            Miasto = sMiasto,
            Dzielnica = sDzielnica,
            Ulica = sUlica,
            Gmina = sGmina,
            Powiat = sPowiat,
            Wojewodztwo = sWojewodztwo,
            Numery = numery
        };

        records.Add(cp);
        return false;
    }
}