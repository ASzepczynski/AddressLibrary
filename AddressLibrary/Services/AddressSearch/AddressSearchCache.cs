// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Cache s³owników dla szybkiego wyszukiwania adresów
    /// </summary>
    public class AddressSearchCache
    {
        private readonly AddressDbContext _context;
        private readonly TextNormalizer _normalizer;
        
        private Dictionary<string, List<Miejscowosc>>? _miejscowosciDict;
        private Dictionary<int, List<Ulica>>? _uliceDict;
        private Dictionary<int, List<KodPocztowy>>? _kodyPocztoweDict;
        private bool _isInitialized;

        public AddressSearchCache(AddressDbContext context, TextNormalizer normalizer)
        {
            _context = context;
            _normalizer = normalizer;
            _isInitialized = false;
        }

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Inicjalizuje wszystkie s³owniki z bazy danych
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            // Za³aduj wszystkie miejscowoœci z pe³n¹ hierarchi¹
            var miejscowosci = await _context.Miejscowosci
                .Include(m => m.Gmina)
                    .ThenInclude(g => g.Powiat)
                        .ThenInclude(p => p.Wojewodztwo)
                .Include(m => m.Gmina.RodzajGminy)
                .Where(m => m.Id != -1)
                .ToListAsync();

            // S³ownik: znormalizowana nazwa miejscowoœci -> lista miejscowoœci
            _miejscowosciDict = miejscowosci
                .GroupBy(m => _normalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Za³aduj wszystkie ulice
            var ulice = await _context.Ulice
                .Include(u => u.Miejscowosc)
                .Where(u => u.Id != -1)
                .ToListAsync();

            // S³ownik: miejscowoœæ ID -> lista ulic
            _uliceDict = ulice
                .GroupBy(u => u.MiejscowoscId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Za³aduj wszystkie kody pocztowe
            var kodyPocztowe = await _context.KodyPocztowe
                .Include(k => k.Miejscowosc)
                .Include(k => k.Ulica)
                .Where(k => k.Id != -1)
                .ToListAsync();

            // S³ownik: miejscowoœæ ID -> lista kodów pocztowych
            _kodyPocztoweDict = kodyPocztowe
                .GroupBy(k => k.MiejscowoscId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _isInitialized = true;
        }

        /// <summary>
        /// Znajduje miejscowoœci o podanej znormalizowanej nazwie
        /// </summary>
        public bool TryGetMiejscowosci(string normalizedName, out List<Miejscowosc> miejscowosci)
        {
            miejscowosci = new List<Miejscowosc>();
            
            if (_miejscowosciDict == null)
                return false;

            return _miejscowosciDict.TryGetValue(normalizedName, out miejscowosci!);
        }

        /// <summary>
        /// Znajduje ulice w podanej miejscowoœci
        /// </summary>
        public bool TryGetUlice(int miejscowoscId, out List<Ulica> ulice)
        {
            ulice = new List<Ulica>();
            
            if (_uliceDict == null)
                return false;

            return _uliceDict.TryGetValue(miejscowoscId, out ulice!);
        }

        /// <summary>
        /// Znajduje kody pocztowe dla podanej miejscowoœci
        /// </summary>
        public bool TryGetKodyPocztowe(int miejscowoscId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            
            if (_kodyPocztoweDict == null)
                return false;

            return _kodyPocztoweDict.TryGetValue(miejscowoscId, out kody!);
        }
    }
}