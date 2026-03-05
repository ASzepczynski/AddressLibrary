// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Helpers;
using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Serwis do rozwiązywania niejednoznaczności przy wyszukiwaniu ulic
    /// </summary>
    public class AmbiguousStreetResolver
    {
        public AmbiguousStreetResolver()
        {
        }
                
        // Prawdziwa nazwa żeby wyświetlić duplikaty
        private string GetOriginalStreetName(UlicaCached street)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(street.Cecha))
                parts.Add(street.Cecha);

            parts.Add(street.Nazwa1);
            if (!string.IsNullOrWhiteSpace(street.Nazwa2))
            {
                parts.Add(street.Nazwa2);
            }
            return string.Join(" ", parts);
        }


        
        /// <summary>
        /// Zwraca szczegółowy komunikat o niejednoznaczności
        /// </summary>
        public string GetAmbiguityMessage(
     List<UlicaCached> streets,
     List<KodPocztowy> postalCodes
 )
        {
            var details = streets.Select(s =>
            {
                var streetName = GetOriginalStreetName(s);
                var streetId = s.Id;
                var dzielnicaStr = !string.IsNullOrWhiteSpace(s.Dzielnica) ? $" [{s.Dzielnica}]" : "";

                var codes = postalCodes
                    .Where(k => k.UlicaId == s.Id)
                    .Select(k => k.Kod)
                    .Distinct()
                    .OrderBy(k => k)
                    .ToList();

                var codesStr = codes.Count > 0
                    ? string.Join(", ", codes)
                    : "(brak kodu)";

                return $"{codesStr} ({streetName}/{streetId}{dzielnicaStr})";
            }).ToList();

            return $"Znaleziono wiele dopasowań ulicy [A] {details.Count}): {string.Join(", ", details)}";
        }
    }
}