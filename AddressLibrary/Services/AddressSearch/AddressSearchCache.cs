// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Cache;
using AddressLibrary.Data;
using AddressLibrary.Dictionaries.Pseudonimy;
using AddressLibrary.Helpers;
using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Cache s³owników dla szybkiego wyszukiwania adresów.
    /// Deleguje do klas z folderu Cache (MiastaCache, UliceCache, KodyPocztoweCache).
    /// </summary>
    public class AddressSearchCache
    {
        private readonly MiastaCache       _miasta;
        private readonly UliceCache        _ulice;
        private readonly KodyPocztoweCache _kodyPocztowe;
        public Dictionary<string, string>  PseudonimiDict { get; }

        public bool IsInitialized =>
            _miasta.IsInitialized && _ulice.IsInitialized && _kodyPocztowe.IsInitialized;

        public AddressSearchCache(AddressDbContext context, string appDataPath)
        {
            _miasta       = new MiastaCache(context);
            _ulice        = new UliceCache(context);
            _kodyPocztowe = new KodyPocztoweCache(context);
            PseudonimiDict = PseudonimiDictionary.Load(appDataPath);
        }

        public async Task InitializeAsync()
        {
            await _miasta.InitializeAsync();
            await _ulice.InitializeAsync();
            await _kodyPocztowe.InitializeAsync();
        }

        public void Invalidate()
        {
            _miasta.Invalidate();
            _ulice.Invalidate();
            _kodyPocztowe.Invalidate();
        }

        // ?? Miasta ???????????????????????????????????????????????????????????

        public bool TryGetMiasta(string normalizedName, out List<Miasto> miasta) =>
            _miasta.TryGet(normalizedName, out miasta);

        public List<Miasto> FindCitiesByName(string cityName) => _miasta.Find(cityName);

        public List<Miasto> GetAllCities() => _miasta.GetAll();

        // ?? Ulice ????????????????????????????????????????????????????????????

        public bool TryGetUlice(int miastoId, out List<UlicaCached> ulice) =>
            _ulice.TryGet(miastoId, out ulice);

        public List<(string MiastoNazwa, string UlicaNazwa)> FindStreetGlobally(string streetName) =>
            _ulice.FindGlobally(streetName);

        public string GetOriginalStreetName(UlicaCached ulica) => ulica.GetDisplayName();

        // ?? Kody pocztowe ????????????????????????????????????????????????????

        public bool TryGetKodyPocztoweMiasta(int miastoId, out List<KodPocztowy> kody) =>
            _kodyPocztowe.TryGetByMiasto(miastoId, out kody);

        public bool TryGetKodyPocztoweUlicy(int ulicaId, out List<KodPocztowy> kody) =>
            _kodyPocztowe.TryGetByUlica(ulicaId, out kody);
    }
}
