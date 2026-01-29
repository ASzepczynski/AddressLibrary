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
        /// </summary>
        public KodPocztowy Build(Pna pna, Miasto miasto, Ulica? ulica)
        {
            return new KodPocztowy
            {
                Kod = pna.Kod,
                Numery = pna.Numery,
                MiastoId = miasto.Id,
                UlicaId = ulica?.Id ?? -1
            };
        }
    }
}