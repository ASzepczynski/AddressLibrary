// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch.Filters
{
    /// <summary>
    /// Filtry dla kodów pocztowych
    /// </summary>
    public class PostalCodeFilters
    {
        private readonly BuildingNumberValidator _numberValidator;

        public PostalCodeFilters(BuildingNumberValidator numberValidator)
        {
            _numberValidator = numberValidator;
        }

        /// <summary>
        /// Filtruje kody pocztowe po ID ulicy
        /// </summary>
        public List<KodPocztowy> FilterByStreet(List<KodPocztowy> kody, int ulicaId)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].UlicaId == ulicaId)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe bez ulicy
        /// </summary>
        public List<KodPocztowy> FilterWithoutStreet(List<KodPocztowy> kody)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].UlicaId == -1 || kody[i].UlicaId == null)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe po numerze budynku
        /// </summary>
        public List<KodPocztowy> FilterByBuildingNumber(List<KodPocztowy> kody, string numerBudynku)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (_numberValidator.IsNumberInRange(numerBudynku, kody[i].Numery))
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Filtruje kody pocztowe po kodzie pocztowym
        /// </summary>
        public List<KodPocztowy> FilterByPostalCode(List<KodPocztowy> kody, string kodPocztowy)
        {
            var result = new List<KodPocztowy>();
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].Kod == kodPocztowy)
                {
                    result.Add(kody[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Znajduje kod miejscowoœci (bez ulicy)
        /// </summary>
        public KodPocztowy? FindCityPostalCode(List<KodPocztowy> kody)
        {
            for (int i = 0; i < kody.Count; i++)
            {
                if (kody[i].UlicaId == -1 || kody[i].UlicaId == null)
                {
                    return kody[i];
                }
            }
            return null;
        }
    }
}