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

            // ✅ POPRAWKA: Załaduj TypUlicy z TytulStopien dla computed properties
            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien)
                .Where(u => u.Id != -1)
                .ToListAsync();

            // ✅ Konwertuj na UlicaCached z pre-znormalizowanymi komponentami
            var uliceCached = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiastoId = u.MiastoId,
                Cecha = u.Cecha ?? "",
                Miasto = u.Miasto,
                Dzielnica = u.Dzielnica,
                TypUlicyId = u.TypUlicyId,

                // 🚀 Pre-normalizuj komponenty z TypUlicy
                // ✅ POPRAWKA: Sprawdzaj TypUlicyId != -1 zamiast null
                Prefiks = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Prefiks)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Prefiks),
                
                // ✅ POPRAWKA: Sprawdzaj TytulStopienId != -1 zamiast TytulStopien == null
                Tytul = u.TypUlicyId == -1 || u.TypUlicy == null || u.TypUlicy.TytulStopienId == -1 || u.TypUlicy.TytulStopien == null
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.TytulStopien.Dopelniacz ?? u.TypUlicy.TytulStopien.Skrot ?? ""),
                
                Imie = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Imie)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Imie),
                
                Imie2 = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Imie2)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Imie2),
                
                Nazwisko = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Nazwisko)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Nazwisko),
                
                Nazwisko2 = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Nazwisko2)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Nazwisko2),
                
                Pseudonim = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Pseudonim)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Pseudonim),
                
                Postfiks = u.TypUlicyId == -1 || u.TypUlicy == null || string.IsNullOrWhiteSpace(u.TypUlicy.Postfiks)
                    ? null
                    : TextNormalizer.Normalize(u.TypUlicy.Postfiks)

            }).ToList();

            // Słownik: miasto ID -> lista ulic (cached)
            _uliceDict = uliceCached
                .GroupBy(u => u.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Załaduj wszystkie kody pocztowe
            var kodyPocztowe = await _context.KodyPocztowe
                .Include(k => k.Miasto)
                .Include(k => k.Ulica)
                .ToListAsync();

            // Słownik: miasto ID -> kody pocztowe dla tego miasta (bez ulicy)
            _kodyPocztoweMiastDict = kodyPocztowe
                .Where(k => k.UlicaId == null || k.UlicaId == -1)
                .GroupBy(k => k.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Słownik: ulica ID -> kody pocztowe dla tej ulicy
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
