using AddressLibrary.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Utils
{
    public static class PrefixModification
    {

        public static (bool zmiana,string cecha, string ulica) 
            ModifyPrefix(string cecha,string ulica,string miasto)
        {
            bool bylaZmiana = false;
            bool zmiana;

            // Jeśli cecha jest inne a ulica jest Most to zmieniamy cechę
            (zmiana, cecha, ulica) = ZmienCeche(cecha, "inne", "most", ulica, miasto);
            bylaZmiana = bylaZmiana || zmiana;
            // Jeśli cecha jest ul. a ulica jest Rynek to zmieniamy cechę

            (zmiana, cecha, ulica) = ZmienCeche(cecha, "ul.", "rynek", ulica, miasto);
            bylaZmiana = bylaZmiana || zmiana;

            (zmiana, cecha, ulica) = ZmienCeche(cecha, "pl.", "rynek", ulica, miasto);
            bylaZmiana = bylaZmiana || zmiana;

            (zmiana, cecha, ulica) = ZmienCeche(cecha, "rynek", "rynek", ulica, miasto);
            bylaZmiana = bylaZmiana || zmiana;

            // Tutaj na wszelki wypadek przywracam Napis Rynek 
            if (cecha == "rynek" && ulica == "")
            {
                ulica = "Rynek";
            }

            return (bylaZmiana, cecha, ulica);
        }

        public static (bool zmiana, string Cecha, string Nazwa) ZmienCeche(
            string curCecha,
            string patCecha,
            string searchString,
            string Nazwa,
            string miastoNazwa)
        {
            TextInfo textInfo = new CultureInfo("pl-PL", false).TextInfo;
            string sInitcap = textInfo.ToTitleCase(searchString.ToLower());


            if (!string.Equals(curCecha, patCecha, StringComparison.OrdinalIgnoreCase))
                return (false, curCecha, Nazwa);


            if ((Nazwa != sInitcap) && !Nazwa.StartsWith(sInitcap + " ", StringComparison.OrdinalIgnoreCase))
                return (false, curCecha, Nazwa);

            var oldCecha = curCecha;
            var oldNazwa1 = Nazwa;
            if (Nazwa == sInitcap)
            {
                Nazwa = "";
            }
            else
            {
                Nazwa = Nazwa.Substring(searchString.Length).Trim();
            }
            curCecha = searchString;

            return (true, curCecha, Nazwa);
        }


    }
}
