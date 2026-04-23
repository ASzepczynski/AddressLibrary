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
                
        /// <summary>
        /// Zwraca prawdziwą nazwę ulicy do wyświetlania duplikatów
        /// ✅ POPRAWKA: Użyj GetDisplayName() zamiast Nazwa1/Nazwa2
        /// </summary>
        private string GetOriginalStreetName(UlicaCached street)
        {
            return street.GetDisplayName();
        }

        /// <summary>
        /// Zwraca szczegółowy komunikat o niejednoznaczności
        /// </summary>
        public string GetAmbiguityMessage(
            AddressSearchRequest request,
            List<UlicaCached> streets,
            List<KodPocztowy> postalCodes)
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

            return $"Znaleziono wiele dopasowań ulicy [{request.Miasto}][{request.Ulica}][A] {details.Count}): {string.Join(", ", details)}";
        }
    }
}