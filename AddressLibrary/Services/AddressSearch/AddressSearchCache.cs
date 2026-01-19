// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Cache słowników dla szybkiego wyszukiwania adresów (z pre-znormalizowanymi danymi)
    /// </summary>
    public class AddressSearchCache
    {
        private readonly AddressDbContext _context;
        private readonly TextNormalizer _normalizer;
        
        private Dictionary<string, List<Miasto>>? _miastaDict;
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

            // Załaduj wszystkie miasta z pełną hierarchią
            var miasta = await _context.Miasta
                .Include(m => m.Gmina)
                    .ThenInclude(g => g.Powiat)
                        .ThenInclude(p => p.Wojewodztwo)
                .Include(m => m.Gmina.RodzajGminy)
                .Where(m => m.Id != -1)
                .ToListAsync();

            // Słownik: znormalizowana nazwa miasta -> lista miast
            _miastaDict = miasta
                .GroupBy(m => _normalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Załaduj wszystkie ulice i stwórz cached wersje
            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Where(u => u.Id != -1)
                .ToListAsync();

            // Konwertuj na UlicaCached z pre-znormalizowanymi nazwami
            var uliceCached = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiastoId = u.MiastoId,
                Cecha = u.Cecha,
                Nazwa1 = u.Nazwa1,
                Nazwa2 = u.Nazwa2,
                Miasto = u.Miasto,
                
                // Pre-znormalizowane nazwy
                NormalizedNazwa1 = _normalizer.Normalize(u.Nazwa1),
                NormalizedNazwa2 = string.IsNullOrEmpty(u.Nazwa2) ? null : _normalizer.Normalize(u.Nazwa2),
                NormalizedCombined = string.IsNullOrEmpty(u.Nazwa2) 
                    ? null 
                    : _normalizer.Normalize($"{u.Nazwa2} {u.Nazwa1}"),
                NormalizedCombinedReverse = string.IsNullOrEmpty(u.Nazwa2)
                    ? null
                    : _normalizer.Normalize($"{u.Nazwa1} {u.Nazwa2}")
            }).ToList();

            // Słownik: miasto ID -> lista ulic (cached)
            _uliceDict = uliceCached
                .GroupBy(u => u.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Załaduj wszystkie kody pocztowe
            var kodyPocztowe = await _context.KodyPocztowe
                .Include(k => k.Miasto)
                .Include(k => k.Ulica)
                .Where(k => k.Id != -1)
                .ToListAsync();

            // Słownik: miasto ID -> lista kodów pocztowych
            _kodyPocztoweDict = kodyPocztowe
                .GroupBy(k => k.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _isInitialized = true;
        }

        /// <summary>
        /// Znajduje miasta o podanej znormalizowanej nazwie
        /// </summary>
        public bool TryGetMiasta(string normalizedName, out List<Miasto> miasta)
        {
            miasta = new List<Miasto>();
            
            if (_miastaDict == null)
                return false;

            return _miastaDict.TryGetValue(normalizedName, out miasta!);
        }

        /// <summary>
        /// Znajduje ulice (cached) w podanym mieście
        /// </summary>
        public bool TryGetUlice(int miastoId, out List<UlicaCached> ulice)
        {
            ulice = new List<UlicaCached>();
            
            if (_uliceDict == null)
                return false;

            return _uliceDict.TryGetValue(miastoId, out ulice!);
        }

        /// <summary>
        /// Znajduje kody pocztowe dla podanego miasta
        /// </summary>
        public bool TryGetKodyPocztowe(int miastoId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            
            if (_kodyPocztoweDict == null)
                return false;

            return _kodyPocztoweDict.TryGetValue(miastoId, out kody!);
        }

        /// <summary>
        /// 🆕 Zwraca oryginalną nazwę ulicy (z cechą, jeśli istnieje)
        /// Używane do wyświetlania nieznormalizowanych nazw w komunikatach
        /// </summary>
        public string GetOriginalStreetName(UlicaCached ulica)
        {
            if (!string.IsNullOrEmpty(ulica.Cecha))
            {
                // Jeśli jest Nazwa2 (np. "Księcia" w "Księcia Józefa")
                if (!string.IsNullOrEmpty(ulica.Nazwa2))
                {
                    return $"{ulica.Cecha} {ulica.Nazwa2} {ulica.Nazwa1}".Trim();
                }
                return $"{ulica.Cecha} {ulica.Nazwa1}".Trim();
            }
            
            // Bez cechy
            if (!string.IsNullOrEmpty(ulica.Nazwa2))
            {
                return $"{ulica.Nazwa2} {ulica.Nazwa1}".Trim();
            }
            
            return ulica.Nazwa1;
        }

        /// <summary>
        /// 🆕 Znajduje ulicę globalnie we WSZYSTKICH miastach (dla diagnostyki)
        /// Zwraca listę lokalizacji, gdzie dana ulica istnieje
        /// </summary>
        public List<(string MiastoNazwa, string UlicaNazwa)> FindStreetGlobally(string normalizedStreetName)
        {
            var locations = new List<(string MiastoNazwa, string UlicaNazwa)>();

            if (_uliceDict == null || string.IsNullOrWhiteSpace(normalizedStreetName))
                return locations;

            // Przeszukaj wszystkie miasta
            foreach (var (miastoId, ulice) in _uliceDict)
            {
                foreach (var ulica in ulice)
                {
                    // Sprawdź czy którakolwiek znormalizowana nazwa pasuje
                    if (ulica.NormalizedNazwa1 == normalizedStreetName ||
                        ulica.NormalizedCombined == normalizedStreetName ||
                        ulica.NormalizedCombinedReverse == normalizedStreetName)
                    {
                        var miastoNazwa = ulica.Miasto?.Nazwa ?? "?";
                        var ulicaNazwa = GetOriginalStreetName(ulica);
                        
                        locations.Add((miastoNazwa, ulicaNazwa));
                    }
                }
            }

            return locations.Distinct().Take(10).ToList(); // Maksymalnie 10 lokalizacji
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
