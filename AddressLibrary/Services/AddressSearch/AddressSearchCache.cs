// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Helpers;
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
        private readonly string _appDataPath;

        private Dictionary<string, List<Miasto>>? _miastaDict;
        private Dictionary<int, List<UlicaCached>>? _uliceDict;
        private Dictionary<int, List<KodPocztowy>>? _kodyPocztoweMiastDict;
        private Dictionary<int, List<KodPocztowy>>? _kodyPocztoweUlicDict;
        private bool _isInitialized;

        public AddressSearchCache(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _isInitialized = false;
        }

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Wymusza ponowną inicjalizację cache przy następnym wywołaniu InitializeAsync.
        /// Należy wywołać po każdej operacji zmieniającej dane w bazie (np. załadowaniu kodów pocztowych).
        /// </summary>
        public void Invalidate()
        {
            _isInitialized = false;
            _miastaDict = null;
            _uliceDict = null;
            _kodyPocztoweMiastDict = null;
            _kodyPocztoweUlicDict = null;
        }

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
                .GroupBy(m => TextNormalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Załaduj ulice z CechaUlicy i TypUlicy (z TytulStopien)
            Console.WriteLine("=== AddressSearchCache: Ładowanie ulic z bazy ===");

            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Include(u => u.CechaUlicy)
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien)
                .Where(u => u.Id != -1)
                .ToListAsync();

            Console.WriteLine($"=== AddressSearchCache: Załadowano {ulice.Count} ulic z bazy ===");

            // 🐛 DEBUG: Sprawdź pierwsze 10 ulic
            int checkedCount = 0;
            int nullCount = 0;
            int validCount = 0;

            foreach (var u in ulice.Take(10))
            {
                checkedCount++;
                if (u.CechaUlicy == null)
                {
                    nullCount++;
                    Console.WriteLine($"⚠️ Ulica Id={u.Id}, Symbol={u.Symbol}, CechaUlicyId={u.CechaUlicyId} => CechaUlicy jest NULL!");
                }
                else
                {
                    validCount++;
                    Console.WriteLine($"✓ Ulica Id={u.Id}, Symbol={u.Symbol}, CechaUlicyId={u.CechaUlicyId}, CechaSkrot='{u.CechaUlicy.Skrot}'");
                }
            }

            Console.WriteLine($"=== DEBUG PODSUMOWANIE: Sprawdzono {checkedCount} ulic, {validCount} OK, {nullCount} NULL ===");

            // Sprawdź statystyki dla wszystkich ulic
            int totalNull = ulice.Count(u => u.CechaUlicy == null);
            int totalValid = ulice.Count(u => u.CechaUlicy != null);
            Console.WriteLine($"=== WSZYSTKIE ULICE: Total={ulice.Count}, Valid={totalValid}, NULL={totalNull} ===");

            // ✅ Konwertuj na UlicaCached z pre-znormalizowanymi komponentami
            var uliceCached = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiastoId = u.MiastoId,
                CechaUlicy = u.CechaUlicy,
                Miasto = u.Miasto,
                Dzielnica = u.Dzielnica ?? string.Empty,
                TypUlicyId = u.TypUlicyId,

                // 🚀 Pre-normalizuj komponenty z TypUlicy (zawsze string.Empty, NIGDY null)
                Prefiks = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Prefiks)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Prefiks),
                
                Tytul = u.TypUlicyId == -1 || u.TypUlicy == null || u.TypUlicy.TytulStopienId == -1 || u.TypUlicy.TytulStopien == null
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.TytulStopien.Dopelniacz ?? u.TypUlicy.TytulStopien.Skrot ?? string.Empty),
                
                Imie = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Imie)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Imie),
                
                Imie2 = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Imie2)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Imie2),
                
                Nazwisko = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Nazwisko)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Nazwisko),
                
                Nazwisko2 = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Nazwisko2)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Nazwisko2),
                
                Pseudonim = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Pseudonim)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Pseudonim),
                
                Postfiks = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Postfiks)
                    ? string.Empty
                    : TextNormalizer.Normalize(u.TypUlicy.Postfiks)

            }).ToList();

            // Słownik: miasto ID -> lista ulic (cached)
            _uliceDict = uliceCached
                .GroupBy(u => u.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Załaduj wszystkie kody pocztowe — wyłącz AutoInclude (generuje JOIN który odcina rekordy z nieistniejącymi FK)
            var kodyPocztowe = await _context.KodyPocztowe
                .IgnoreAutoIncludes()
                .AsNoTracking()
                .ToListAsync();

            // Słownik: miasto ID -> kody pocztowe dla wszystkich miast (z ulicą lub bez)
            _kodyPocztoweMiastDict = kodyPocztowe
//                .Where(k => k.UlicaId == -1)
                .GroupBy(k => k.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

         

            // Słownik: ulica ID -> kody pocztowe dla wszystkich ulic
            _kodyPocztoweUlicDict = kodyPocztowe
                .Where(k => k.UlicaId != -1)
                .GroupBy(k => k.UlicaId)
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
        /// Znajduje miasta o podanej nazwie (automatyczna normalizacja)
        /// </summary>
        public List<Miasto> FindCitiesByName(string cityName)
        {
            var normalized = TextNormalizer.Normalize(cityName);
            
            if (TryGetMiasta(normalized, out var miasta))
            {
                return miasta;
            }
            
            return new List<Miasto>();
        }

        /// <summary>
        /// Zwraca wszystkie miasta z cache
        /// </summary>
        public List<Miasto> GetAllCities()
        {
            if (_miastaDict == null)
                return new List<Miasto>();

            return _miastaDict.Values
                .SelectMany(lista => lista)
                .ToList();
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
        public bool TryGetKodyPocztoweMiasta(int miastoId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();

            if (_kodyPocztoweMiastDict == null)
                return false;

            if (miastoId == 666633)
            {
                int v = 1;
            }

            return _kodyPocztoweMiastDict.TryGetValue(miastoId, out kody!);
        }

        /// <summary>
        /// Znajduje kody pocztowe dla podanej ulicy
        /// </summary>
        public bool TryGetKodyPocztoweUlicy(int ulicaId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();

            if (_kodyPocztoweUlicDict == null)
                return false;

            return _kodyPocztoweUlicDict.TryGetValue(ulicaId, out kody!);
        }

        /// <summary>
        /// Zwraca oryginalną nazwę ulicy (z cechą, dla wyświetlania)
        /// </summary>
        public string GetOriginalStreetName(UlicaCached ulica)
        {
            return ulica.GetDisplayName();
        }

        /// <summary>
        /// 🆕 Znajduje ulicę globalnie we WSZYSTKICH miastach (dla diagnostyki)
        /// Zwraca listę lokalizacji, gdzie dana ulica istnieje
        /// </summary>
        public List<(string MiastoNazwa, string UlicaNazwa)> FindStreetGlobally(string streetName)
        {
            var locations = new List<(string MiastoNazwa, string UlicaNazwa)>();

            if (_uliceDict == null || string.IsNullOrWhiteSpace(streetName))
                return locations;

            var normalized = TextNormalizer.Normalize(streetName);

            // Przeszukaj wszystkie miasta
            foreach (var (miastoId, ulice) in _uliceDict)
            {
                foreach (var ulica in ulice)
                {
                    // Sprawdź czy pełna nazwa znormalizowana zawiera wyszukiwaną fragment
                    var fullNormalized = ulica.GetFullNormalized();
                    
                    // Dopasowanie:
                    // 1. Pełna nazwa zawiera fragment
                    // 2. Nazwisko pasuje (dla dokładniejszych wyników)
                    if (fullNormalized.Contains(normalized) || 
                        (ulica.Nazwisko != null && ulica.Nazwisko.Contains(normalized)))
                    {
                        var miastoNazwa = ulica.Miasto?.Nazwa ?? "Nieznane miasto";
                        var ulicaNazwa = ulica.GetDisplayName();
                        
                        locations.Add((miastoNazwa, ulicaNazwa));
                    }
                }
            }

            return locations;
        }
    }
}
