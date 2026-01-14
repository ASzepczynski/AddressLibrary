// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Cache słowników dla szybkiego wyszukiwania adresów
    /// </summary>
    public class AddressSearchCache
    {
        private readonly AddressDbContext _context;
        private readonly TextNormalizer _normalizer;
        
        private Dictionary<string, List<Miejscowosc>>? _miejscowosciDict;
        private Dictionary<int, List<UlicaCached>>? _uliceDict;
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
        /// Inicjalizuje wszystkie słowniki z bazy danych
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            Console.WriteLine("[AddressSearchCache] Rozpoczynam inicjalizację cache...");
            var totalStartTime = DateTime.Now;

            // ===== MIEJSCOWOŚCI =====
            var miejscowosciStartTime = DateTime.Now;
            var miejscowosci = await _context.Miejscowosci
                .Include(m => m.Gmina)
                    .ThenInclude(g => g.Powiat)
                        .ThenInclude(p => p.Wojewodztwo)
                .Include(m => m.Gmina.RodzajGminy)
                .Where(m => m.Id != -1)
                .ToListAsync();
            var miejscowosciTime = (DateTime.Now - miejscowosciStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {miejscowosci.Count} miejscowości w {miejscowosciTime:F2}s");

            // Słownik: znormalizowana nazwa miejscowości -> lista miejscowości
            _miejscowosciDict = miejscowosci
                .GroupBy(m => _normalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());

            // ===== ULICE =====
            var uliceStartTime = DateTime.Now;
            var ulice = await _context.Ulice
                .Include(u => u.Miejscowosc)
                .Where(u => u.Id != -1)
                .ToListAsync();
            var uliceLoadTime = (DateTime.Now - uliceStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {ulice.Count} ulic w {uliceLoadTime:F2}s");
            
            // 🚀 OPTYMALIZACJA: Znormalizuj nazwy ulic raz przy ładowaniu
            var normalizeStartTime = DateTime.Now;
            var cachedUlice = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiejscowoscId = u.MiejscowoscId,
                Cecha = u.Cecha,
                Nazwa1 = u.Nazwa1,
                Nazwa2 = u.Nazwa2,
                Miejscowosc = u.Miejscowosc,
                // Znormalizowane wersje - obliczone raz!
                NormalizedNazwa1 = _normalizer.Normalize(u.Nazwa1),
                NormalizedNazwa2 = string.IsNullOrEmpty(u.Nazwa2) ? null : _normalizer.Normalize(u.Nazwa2),
                NormalizedCombined = string.IsNullOrEmpty(u.Nazwa2) 
                    ? null 
                    : _normalizer.Normalize($"{u.Nazwa2} {u.Nazwa1}"),
                NormalizedCombinedReverse = string.IsNullOrEmpty(u.Nazwa2)
                    ? null
                    : _normalizer.Normalize($"{u.Nazwa1} {u.Nazwa2}")
            }).ToList();
            
            var normalizeTime = (DateTime.Now - normalizeStartTime).TotalSeconds;
            Console.WriteLine($"[AddressSearchCache] Znormalizowano nazwy ulic w {normalizeTime:F2}s");

            // Słownik: miejscowość ID -> lista ulic (z cache)
            _uliceDict = cachedUlice
                .GroupBy(u => u.MiejscowoscId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ===== KODY POCZTOWE =====
            var kodyStartTime = DateTime.Now;
            var kodyPocztowe = await _context.KodyPocztowe
                .Include(k => k.Miejscowosc)
                .Include(k => k.Ulica)
                .Where(k => k.Id != -1)
                .ToListAsync();
            var kodyLoadTime = (DateTime.Now - kodyStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {kodyPocztowe.Count} kodów pocztowych w {kodyLoadTime:F2}s");

            // Słownik: miejscowość ID -> lista kodów pocztowych
            _kodyPocztoweDict = kodyPocztowe
                .GroupBy(k => k.MiejscowoscId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var totalTime = (DateTime.Now - totalStartTime).TotalSeconds;
            Console.WriteLine($"[AddressSearchCache] ✓ Cache zainicjowany w {totalTime:F2}s " +
                             $"(miejscowości: {miejscowosciTime:F1}s, ulice: {uliceLoadTime:F1}s + normalizacja: {normalizeTime:F1}s, kody: {kodyLoadTime:F1}s)");

            _isInitialized = true;
        }

        /// <summary>
        /// Znajduje miejscowości o podanej znormalizowanej nazwie
        /// </summary>
        public bool TryGetMiejscowosci(string normalizedName, out List<Miejscowosc> miejscowosci)
        {
            miejscowosci = new List<Miejscowosc>();
            
            if (_miejscowosciDict == null)
                return false;

            return _miejscowosciDict.TryGetValue(normalizedName, out miejscowosci!);
        }

        /// <summary>
        /// Znajduje ulice w podanej miejscowości (z cache'owanymi znormalizowanymi nazwami)
        /// </summary>
        public bool TryGetUlice(int miejscowoscId, out List<UlicaCached> ulice)
        {
            ulice = new List<UlicaCached>();
            
            if (_uliceDict == null)
                return false;

            return _uliceDict.TryGetValue(miejscowoscId, out ulice!);
        }

        /// <summary>
        /// Znajduje kody pocztowe dla podanej miejscowości
        /// </summary>
        public bool TryGetKodyPocztowe(int miejscowoscId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            
            if (_kodyPocztoweDict == null)
                return false;

            return _kodyPocztoweDict.TryGetValue(miejscowoscId, out kody!);
        }

        /// <summary>
        /// 🆕 Znajduje wszystkie miejscowości w podanej gminie
        /// </summary>
        public List<Miejscowosc> GetMiejscowosciInGmina(int gminaId)
        {
            if (_miejscowosciDict == null)
                return new List<Miejscowosc>();

            // Przeszukaj wszystkie miejscowości i zwróć te z danej gminy
            var result = new List<Miejscowosc>();
            foreach (var kvp in _miejscowosciDict)
            {
                foreach (var miejscowosc in kvp.Value)
                {
                    if (miejscowosc.GminaId == gminaId)
                    {
                        result.Add(miejscowosc);
                    }
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// 🚀 Cached wersja Ulica z pre-znormalizowanymi nazwami
    /// </summary>
    public class UlicaCached
    {
        public int Id { get; set; }
        public int MiejscowoscId { get; set; }
        public string Cecha { get; set; } = string.Empty;
        public string Nazwa1 { get; set; } = string.Empty;
        public string? Nazwa2 { get; set; }
        public Miejscowosc Miejscowosc { get; set; } = null!;

        // 🚀 Pre-znormalizowane nazwy (obliczone raz przy inicjalizacji)
        public string NormalizedNazwa1 { get; set; } = string.Empty;
        public string? NormalizedNazwa2 { get; set; }
        public string? NormalizedCombined { get; set; }
        public string? NormalizedCombinedReverse { get; set; }
    }
}