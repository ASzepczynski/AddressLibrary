// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Buduje słowniki pomocnicze dla procesowania kodów pocztowych
    /// </summary>
    internal class KodyPocztoweDictionaryBuilder
    {
        private readonly AddressDbContext _context;

        public KodyPocztoweDictionaryBuilder(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tworzy słownik gmin: "Województwo|Powiat|Gmina" -> Lista<Gmina>
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
        /// Tworzy słownik miast: GminaId -> Dictionary[Nazwa -> Miasto]
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, Miasto>>> BuildMiastaDictionaryAsync()
        {
            // var miastaList = await _context.Miasta.ToListAsync();
            var miastaList = await _context.Miasta
        .Include(m => m.RodzajMiasta) // <-- to jest kluczowe!
        .ToListAsync();


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
        /// Tworzy słownik ulic: MiastoId -> Dictionary[Nazwa -> Ulica]
        /// Obsługuje zarówno Nazwa1 jak i "Nazwa2 Nazwa1"
        /// 
        /// ⚠️ WYJĄTEK: 
        /// NIE dodawaj klucza tylko Nazwa1, aby uniknąć kolizji z krótszymi nazwami.
        /// 
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

                // 🆕 Sprawdź czy Nazwa2 jest specjalnym prefiksem
                bool hasSpecialPrefix = !string.IsNullOrWhiteSpace(ulica.Nazwa2) && Wyjatek(ulica);

                // KROK 1: Dodaj wpis dla Nazwa1 TYLKO jeśli NIE ma specjalnego prefiksu
                if (!hasSpecialPrefix)
                {
                    var nazwa1Lower = ulica.Nazwa1.ToLowerInvariant();
                    if (!ulice.ContainsKey(nazwa1Lower))
                    {
                        ulice[nazwa1Lower] = ulica;
                    }
                }

                // KROK 2: Jeśli Nazwa2 istnieje, dodaj także klucz "Nazwa2 Nazwa1"
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

        /// <summary>
        /// Sprawdza czy ulica wymaga specjalnego traktowania (nie dodawaj klucza Nazwa1)
        /// </summary>
        /// <param name="ulica">Ulica do sprawdzenia</param>
        /// <returns>True jeśli ulica ma specjalny prefiks wymagający pełnej nazwy</returns>
        private static bool Wyjatek(Ulica ulica)
        {
            // Specjalny przypadek: "Księcia Józefa"
            if (ulica.Nazwa1.Equals("Józefa", StringComparison.OrdinalIgnoreCase) &&
                ulica.Nazwa2.Equals("Księcia", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Tutaj można dodać więcej wyjątków w przyszłości
            // np. "Generała Andersa", "Marszałka Piłsudskiego" itp.

            return false;
        }
    }
}