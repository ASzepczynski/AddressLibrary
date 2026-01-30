// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Tworzy rekordy KodPocztowy z walidacj¹ i obs³ug¹ duplikatów
    /// </summary>
    internal class KodPocztowyRecordBuilder
    {
        

        /// <summary>
        /// Tworzy nowy rekord KodPocztowy
        /// Poprawia b³êdy PNA
        /// </summary>
        public KodPocztowy Build(Pna pna, Miasto miasto, Ulica? ulica)
        {
            string sNumery = pna.Numery;
            if (pna.Miasto == "Kraków" && pna.Ulica == "Tuchowska" && pna.Numery=="1-DK" && pna.Kod=="30-698")
            {
                sNumery = "45-DK(n) 50-DK(p)";
            }
            if (pna.Miasto == "Kraków" && pna.Ulica == "Turniejowa" && pna.Numery == "57-DK" && pna.Kod == "30-619")
            {
                sNumery = "57-59(n) 61-DK(n) 24-DK(p)";
            }
            if (pna.Miasto == "Kraków" && pna.Ulica == "Zakopiañska" && pna.Numery == "33-147(n),46-58b(p), 62-70a(p)" && pna.Kod == "30-418")
            {
                sNumery = "33-147(n),46-58b(p),62(p),64-70a(p)";
            }
            if (pna.Miasto == "Kraków" && pna.Ulica == "Zakopiañska" && pna.Numery == "60-62b(p),72-264(p), 149-DK(n), 265-277" && pna.Kod == "30-435")
            {
                sNumery = "60,62a-62b(p),72-264(p),149-DK(n),265-277";
            }
            

            return new KodPocztowy
            {
                Kod = pna.Kod,
                Numery = sNumery,
                MiastoId = miasto.Id,
                UlicaId = ulica?.Id ?? -1
            };
        }
    }
}