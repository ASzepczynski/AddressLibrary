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
        
        private Dictionary<string, List<Miasto>>? _miastaDict;
        private Dictionary<int, List<UlicaCached>>? _uliceDict;
        private Dictionary<int, List<KodPocztowy>>? _kodyPocztoweDict;
        
        // 🆕 Globalny indeks: znormalizowana nazwa ulicy -> lista miejscowości gdzie występuje
        private Dictionary<string, List<(int MiastoId, string MiastoNazwa, string UlicaNazwa)>>? _globalStreetIndex;
        
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
            var miastaStartTime = DateTime.Now;
            var miasta = await _context.Miasta
                .Include(m => m.Gmina)
                    .ThenInclude(g => g.Powiat)
                        .ThenInclude(p => p.Wojewodztwo)
                .Include(m => m.Gmina.RodzajGminy)
                .Where(m => m.Id != -1)
                .ToListAsync();
            var miastaTime = (DateTime.Now - miastaStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {miasta.Count} miejscowości w {miastaTime:F2}s");

            // Słownik: znormalizowana nazwa miejscowości -> lista miejscowości
            _miastaDict = miasta
                .GroupBy(m => _normalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());

            // ===== ULICE =====
            var uliceStartTime = DateTime.Now;
            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Where(u => u.Id != -1)
                .ToListAsync();
            var uliceLoadTime = (DateTime.Now - uliceStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {ulice.Count} ulic w {uliceLoadTime:F2}s");
            
            var normalizeStartTime = DateTime.Now;
            var cachedUlice = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiastoId = u.MiastoId,
                Cecha = u.Cecha,
                Nazwa1 = u.Nazwa1,
                Nazwa2 = u.Nazwa2,
                Miasto = u.Miasto,
                NormalizedNazwa1 = _normalizer.Normalize(u.Nazwa1),
                NormalizedNazwa2 = string.IsNullOrEmpty(u.Nazwa2) ? null : _normalizer.Normalize(u.Nazwa2),
                
                // 🔧 POPRAWKA: Normalizuj BEZ cechy/tytułu
                NormalizedCombined = string.IsNullOrEmpty(u.Nazwa2) 
                    ? null 
                    : _normalizer.Normalize($"{u.Nazwa2} {u.Nazwa1}"), // np. "plk francesco nullo"
                
                NormalizedCombinedReverse = string.IsNullOrEmpty(u.Nazwa2)
                    ? null
                    : _normalizer.Normalize($"{u.Nazwa1} {u.Nazwa2}") // np. "nullo plk francesco"
            }).ToList();
            
            var normalizeTime = (DateTime.Now - normalizeStartTime).TotalSeconds;
            Console.WriteLine($"[AddressSearchCache] Znormalizowano nazwy ulic w {normalizeTime:F2}s");

            _uliceDict = cachedUlice
                .GroupBy(u => u.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 🆕 BUDUJ GLOBALNY INDEKS ULIC
            var indexStartTime = DateTime.Now;
            _globalStreetIndex = new Dictionary<string, List<(int, string, string)>>();
            
            foreach (var ulica in cachedUlice)
            {
                var miastoNazwa = ulica.Miasto?.Nazwa ?? "?";
                var ulicaNazwa = $"{ulica.Cecha} {ulica.Nazwa1}";
                
                // Indeksuj po NormalizedNazwa1
                if (!_globalStreetIndex.ContainsKey(ulica.NormalizedNazwa1))
                    _globalStreetIndex[ulica.NormalizedNazwa1] = new List<(int, string, string)>();
                _globalStreetIndex[ulica.NormalizedNazwa1].Add((ulica.MiastoId, miastoNazwa, ulicaNazwa));
                
                // Indeksuj po NormalizedNazwa2
                if (ulica.NormalizedNazwa2 != null)
                {
                    if (!_globalStreetIndex.ContainsKey(ulica.NormalizedNazwa2))
                        _globalStreetIndex[ulica.NormalizedNazwa2] = new List<(int, string, string)>();
                    _globalStreetIndex[ulica.NormalizedNazwa2].Add((ulica.MiastoId, miastoNazwa, ulicaNazwa));
                }
            }
            
            var indexTime = (DateTime.Now - indexStartTime).TotalSeconds;
            Console.WriteLine($"[AddressSearchCache] Zbudowano globalny indeks ulic ({_globalStreetIndex.Count} unikalnych nazw) w {indexTime:F2}s");

            // ===== KODY POCZTOWE =====
            var kodyStartTime = DateTime.Now;
            var kodyPocztowe = await _context.KodyPocztowe
                .Include(k => k.Miasto)
                .Include(k => k.Ulica)
                .Where(k => k.Id != -1)
                .ToListAsync();
            var kodyLoadTime = (DateTime.Now - kodyStartTime).TotalSeconds;

            Console.WriteLine($"[AddressSearchCache] Załadowano {kodyPocztowe.Count} kodów pocztowych w {kodyLoadTime:F2}s");

            // Słownik: miejscowość ID -> lista kodów pocztowych
            _kodyPocztoweDict = kodyPocztowe
                .GroupBy(k => k.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var totalTime = (DateTime.Now - totalStartTime).TotalSeconds;
            Console.WriteLine($"[AddressSearchCache] ✓ Cache zainicjowany w {totalTime:F2}s");

            _isInitialized = true;
        }

        /// <summary>
        /// Znajduje miejscowości o podanej znormalizowanej nazwie
        /// </summary>
        public bool TryGetMiasta(string normalizedName, out List<Miasto> miasta)
        {
            miasta = new List<Miasto>();
            
            if (_miastaDict == null)
                return false;

            return _miastaDict.TryGetValue(normalizedName, out miasta!);
        }

        /// <summary>
        /// Znajduje ulice w podanej miejscowości (z cache'owanymi znormalizowanymi nazwami)
        /// </summary>
        public bool TryGetUlice(int miastoId, out List<UlicaCached> ulice)
        {
            ulice = new List<UlicaCached>();
            
            if (_uliceDict == null)
                return false;

            return _uliceDict.TryGetValue(miastoId, out ulice!);
        }

        /// <summary>
        /// Znajduje kody pocztowe dla podanej miejscowości
        /// </summary>
        public bool TryGetKodyPocztowe(int miastoId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            
            if (_kodyPocztoweDict == null)
                return false;

            return _kodyPocztoweDict.TryGetValue(miastoId, out kody!);
        }

        /// <summary>
        /// 🆕 Znajduje wszystkie miejscowości w podanej gminie
        /// </summary>
        public List<Miasto> GetMiastaInGmina(int gminaId)
        {
            if (_miastaDict == null)
                return new List<Miasto>();

            // Przeszukaj wszystkie miejscowości i zwróć te z danej gminy
            var result = new List<Miasto>();
            foreach (var kvp in _miastaDict)
            {
                foreach (var miasto in kvp.Value)
                {
                    if (miasto.GminaId == gminaId)
                    {
                        result.Add(miasto);
                    }
                }
            }
            
            return result;
        }

        // 🆕 NOWA METODA: Znajdź miejscowości gdzie występuje ulica o podanej nazwie
        public List<(int MiastoId, string MiastoNazwa, string UlicaNazwa)> FindStreetGlobally(string normalizedStreetName)
        {
            if (_globalStreetIndex == null)
                return new List<(int, string, string)>();

            if (_globalStreetIndex.TryGetValue(normalizedStreetName, out var locations))
            {
                return locations.Take(5).ToList(); // Max 5 przykładów
            }

            return new List<(int, string, string)>();
        }
    }

    /// <summary>
    /// 🚀 Cached wersja Ulica z pre-znormalizowanymi nazwami
    /// </summary>
    public class UlicaCached
    {
        public int Id { get; set; }
        public int MiastoId { get; set; }
        public string Cecha { get; set; } = string.Empty;
        public string Nazwa1 { get; set; } = string.Empty;
        public string? Nazwa2 { get; set; }
        public Miasto Miasto { get; set; } = null!;

        // 🚀 Pre-znormalizowane nazwy (obliczone raz przy inicjalizacji)
        public string NormalizedNazwa1 { get; set; } = string.Empty;
        public string? NormalizedNazwa2 { get; set; }
        public string? NormalizedCombined { get; set; }
        public string? NormalizedCombinedReverse { get; set; }
    }
}