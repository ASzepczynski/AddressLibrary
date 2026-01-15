// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Buduje s³owniki pomocnicze dla procesowania kodów pocztowych
    /// </summary>
    internal class KodyPocztoweDictionaryBuilder
    {
        private readonly AddressDbContext _context;

        public KodyPocztoweDictionaryBuilder(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tworzy s³ownik gmin: "Województwo|Powiat|Gmina" -> Lista<Gmina>
        /// </summary>
        public async Task<Dictionary<string, List<Gmina>>> BuildGminyDictionaryAsync()
        {
            var gminyAllList = await _context.Gminy
                .Include(g => g.Powiat)
                    .ThenInclude(p => p.Wojewodztwo)
                .Include(g => g.RodzajGminy)
                .ToListAsync();

            return gminyAllList
                .GroupBy(g => $"{g.Powiat.Wojewodztwo.Nazwa}|{g.Powiat.Nazwa}|{g.Nazwa}".ToLowerInvariant())
                .ToDictionary(
                    grp => grp.Key,
                    grp => grp.ToList(),
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Tworzy s³ownik miejscowoœci: GminaId -> Dictionary[Nazwa -> Miasto]
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, Miasto>>> BuildMiastaDictionaryAsync()
        {
            var miastaList = await _context.Miasta.ToListAsync();

            return miastaList
                .GroupBy(m => m.GminaId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(m => m.Nazwa.ToLowerInvariant())
                          .ToDictionary(
                              grp => grp.Key,
                              grp => grp.First(),
                              StringComparer.OrdinalIgnoreCase
                          )
                );
        }

        /// <summary>
        /// Tworzy s³ownik ulic: MiastoId -> Dictionary[Nazwa -> Ulica]
        /// Obs³uguje zarówno Nazwa1 jak i "Nazwa2 Nazwa1"
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, Ulica>>> BuildUliceDictionaryAsync()
        {
            var uliceAllList = await _context.Ulice.ToListAsync();
            var uliceDict = new Dictionary<int, Dictionary<string, Ulica>>();

            foreach (var ulica in uliceAllList)
            {
                if (!uliceDict.ContainsKey(ulica.MiastoId))
                {
                    uliceDict[ulica.MiastoId] = new Dictionary<string, Ulica>(StringComparer.OrdinalIgnoreCase);
                }

                var ulice = uliceDict[ulica.MiastoId];

                // Dodaj wpis dla Nazwa1
                var nazwa1Lower = ulica.Nazwa1.ToLowerInvariant();
                if (!ulice.ContainsKey(nazwa1Lower))
                {
                    ulice[nazwa1Lower] = ulica;
                }

                // Jeœli Nazwa2 istnieje, dodaj tak¿e klucz "Nazwa2 Nazwa1"
                if (!string.IsNullOrWhiteSpace(ulica.Nazwa2))
                {
                    var nazwa2Plus1 = $"{ulica.Nazwa2} {ulica.Nazwa1}".ToLowerInvariant();
                    if (!ulice.ContainsKey(nazwa2Plus1))
                    {
                        ulice[nazwa2Plus1] = ulica;
                    }
                }
            }

            return uliceDict;
        }
    }
}