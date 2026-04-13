// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.KodyPocztoweLoader
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
        /// Tworzy słownik miast: GminaId -> Dictionary[Nazwa -> List&lt;Miasto&gt;]
        /// Lista bo w jednej gminie mogą istnieć dwa miasta o tej samej nazwie.
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, List<Miasto>>>> BuildMiastaDictionaryAsync()
        {
            var miastaList = await _context.Miasta
                .Include(m => m.RodzajMiasta)
                .ToListAsync();

            return miastaList
                .GroupBy(m => m.GminaId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(m => m.Nazwa.ToLowerInvariant())
                          .ToDictionary(
                              grp => grp.Key,
                              grp => grp.ToList(),
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
        /// ✅ EAGER LOADING: Ładuje relację KodyPocztowe, TypUlicy, TytulStopien i CechaUlicy
        /// </summary>
        public async Task<Dictionary<int, Dictionary<string, List<Ulica>>>> BuildUliceDictionaryAsync()
        {
            // ✅ POPRAWKA: Dodano .Include(u => u.CechaUlicy)
            var uliceAllList = await _context.Ulice
                .Include(u => u.KodyPocztowe)
                .Include(u => u.CechaUlicy)  // ✅ DODANE
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien)
                .ToListAsync();

            var uliceDict = new Dictionary<int, Dictionary<string, List<Ulica>>>();

            foreach (var ulica in uliceAllList)
            {
                if (!uliceDict.ContainsKey(ulica.MiastoId))
                {
                    uliceDict[ulica.MiastoId] = new Dictionary<string, List<Ulica>>(StringComparer.OrdinalIgnoreCase);
                }

                var ulice = uliceDict[ulica.MiastoId];

                // Sprawdź czy Nazwa2 jest specjalnym prefiksem
                bool hasSpecialPrefix = !string.IsNullOrWhiteSpace(ulica.Nazwa2) && Wyjatek(ulica);

                // KROK 1: Dodaj wpis dla Nazwa1 TYLKO jeśli NIE ma specjalnego prefiksu
                if (!hasSpecialPrefix)
                {
                    string nazwa1Lower = ulica.Nazwa1.ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(nazwa1Lower))
                    {
                        if (!ulice.ContainsKey(nazwa1Lower))
                        {
                            ulice[nazwa1Lower] = new List<Ulica>();
                        }
                        ulice[nazwa1Lower].Add(ulica);
                    }
                }

                // KROK 2: Jeśli Nazwa2 istnieje, dodaj także klucz "Nazwa2 Nazwa1"
                if (!string.IsNullOrWhiteSpace(ulica.Nazwa2))
                {
                    var nazwa2Plus1 = $"{ulica.Nazwa2} {ulica.Nazwa1}".ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(nazwa2Plus1))
                    {
                        if (!ulice.ContainsKey(nazwa2Plus1))
                        {
                            ulice[nazwa2Plus1] = new List<Ulica>();
                        }
                        ulice[nazwa2Plus1].Add(ulica);
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