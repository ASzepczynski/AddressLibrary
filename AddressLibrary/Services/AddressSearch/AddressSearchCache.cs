// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
        private HashSet<string>? _personalStreets;
        private bool _isInitialized;

        public AddressSearchCache(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _isInitialized = false;
        }

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Zwraca zbiór znormalizowanych nazw ulic osobowych
        /// </summary>
        public HashSet<string> PersonalStreets => _personalStreets ?? new HashSet<string>();

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

            // ✅ POPRAWKA: Załaduj TypUlicy z TytulStopien dla computed properties Nazwa1/Nazwa2
            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien)
                .Where(u => u.Id != -1)
                .ToListAsync();

            // ✅ Konwertuj na UlicaCached z pre-znormalizowanymi nazwami
            var uliceCached = ulice.Select(u => new UlicaCached
            {
                Id = u.Id,
                MiastoId = u.MiastoId,
                Cecha = u.Cecha,
                Nazwa1 = u.Nazwa1,
                Nazwa2 = u.Nazwa2,
                Miasto = u.Miasto,
                Dzielnica = u.Dzielnica,

                // ✅ POPRAWKA 1: Normalizuj tylko Nazwa1 (nazwisko)
                NormalizedNazwa1 = TextNormalizer.Normalize(u.Nazwa1),

                // ✅ POPRAWKA 2: Jeśli jest Nazwa2, normalizuj jako "Nazwa2 Nazwa1" (bez NormalizeOrdinalNumber!)
                NormalizedCombined = string.IsNullOrEmpty(u.Nazwa2)
                    ? null
                    : TextNormalizer.Normalize($"{u.Nazwa2} {u.Nazwa1}")

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

            // Załaduj ulice osobowe
            _personalStreets = LoadPersonalStreets();

            _isInitialized = true;
        }

        /// <summary>
        /// Załaduj ulice osobowe z pliku Excel (zachowaj oryginalną nazwę)
        /// </summary>
        private HashSet<string> LoadPersonalStreets()
        {
            var personalStreets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var excelPath = Path.Combine(_appDataPath, "AppData", "Updates", "UliceOsobowe.xlsx");

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"⚠️ Plik {excelPath} nie istnieje");
                return personalStreets;
            }

            try
            {
                using (var spreadsheet = SpreadsheetDocument.Open(excelPath, false))
                {
                    var workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart == null)
                        return personalStreets;

                    var worksheetPart = workbookPart.WorksheetParts.First();
                    var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    foreach (var row in sheetData.Elements<Row>().Skip(1))
                    {
                        var cells = row.Elements<Cell>().ToList();

                        if (cells.Count >= 5)
                        {
                            string? streetName = GetCellValue(workbookPart, cells[4]);
                            if (!string.IsNullOrWhiteSpace(streetName))
                            {
                                var normalized = TextNormalizer.Normalize(streetName);
                                personalStreets.Add(normalized);
                            }
                        }
                    }
                }

                Console.WriteLine($"✓ Załadowano {personalStreets.Count} ulic osobowych z {excelPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Błąd ładowania ulic osobowych: {ex.Message}");
            }

            return personalStreets;
        }

        private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell.CellValue == null)
                return string.Empty;

            string value = cell.CellValue.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTable = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                if (stringTable != null)
                {
                    return stringTable.SharedStringTable.ElementAt(int.Parse(value)).InnerText;
                }
            }

            return value;
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
        /// 🆕 Zwraca oryginalną nazwę ulicy (z cechą, jeśli istnieje)
        /// Używane do wyświetlania nieznormalizowanych nazw w komunikatach
        /// </summary>
        public string GetOriginalStreetName(UlicaCached ulica)
        {
            return $"{ulica.Cecha} {ulica.Nazwa2} {ulica.Nazwa1}".Replace("  ", " ").Trim();
        }

        /// <summary>
        /// 🆕 Znajduje ulicę globalnie we WSZYSTKICH miastach (dla diagnostyki)
        /// Zwraca listę lokalizacji, gdzie dana ulica istnieje
        /// OBSŁUGUJE także częściowe dopasowanie (skróty jak "Boh." → "Bohaterów")
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
                    bool isMatch = false;

                    // ✅ 1. DOKŁADNE dopasowanie
                    if (ulica.NormalizedNazwa1 == normalizedStreetName ||
                        ulica.NormalizedCombined == normalizedStreetName)
                    {
                        isMatch = true;
                    }

                    // ✅ 2. CZĘŚCIOWE dopasowanie (dla skrótów)
                    if (!isMatch && normalizedStreetName.Length >= 3)
                    {
                        if (ulica.NormalizedCombined != null)
                        {
                            var searchWords = normalizedStreetName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var streetWords = ulica.NormalizedCombined.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                            if (searchWords.Length > 0 && streetWords.Length >= searchWords.Length)
                            {
                                bool allWordsMatch = true;
                                for (int i = 0; i < searchWords.Length; i++)
                                {
                                    if (!streetWords[i].StartsWith(searchWords[i]) &&
                                        !searchWords[i].StartsWith(streetWords[i]))
                                    {
                                        allWordsMatch = false;
                                        break;
                                    }
                                }

                                if (allWordsMatch)
                                {
                                    isMatch = true;
                                }
                            }
                        }
                    }

                    if (isMatch)
                    {
                        var miastoNazwa = ulica.Miasto?.Nazwa ?? "?";
                        var ulicaNazwa = GetOriginalStreetName(ulica);

                        locations.Add((miastoNazwa, ulicaNazwa));
                    }
                }
            }

            return locations.Distinct().Take(10).ToList();
        }

        /// <summary>
        /// 🆕 Znajduje miasta po znormalizowanej nazwie
        /// </summary>
        public List<Miasto> FindCitiesByName(string normalizedCityName)
        {
            if (_miastaDict == null || string.IsNullOrWhiteSpace(normalizedCityName))
                return new List<Miasto>();

            if (_miastaDict.TryGetValue(normalizedCityName, out var miasta))
            {
                return miasta;
            }

            return new List<Miasto>();
        }

        /// <summary>
        /// 🆕 Zwraca wszystkie miejscowości z cache (dla fuzzy matching)
        /// Każda miejscowość ma dodane pole NormalizedNazwa
        /// </summary>
        public List<MiastoCached> GetAllCities()
        {
            if (_miastaDict == null)
                return new List<MiastoCached>();

            // Przekształć słownik miast na listę z znormalizowanymi nazwami
            var allCities = new List<MiastoCached>();

            foreach (var (normalizedName, cities) in _miastaDict)
            {
                foreach (var city in cities)
                {
                    allCities.Add(new MiastoCached
                    {
                        Miasto = city,
                        NormalizedNazwa = normalizedName
                    });
                }
            }

            return allCities;
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
        public string Dzielnica { get; set; } = null!;

        // 🚀 Pre-znormalizowane nazwy
        public string NormalizedNazwa1 { get; set; } = string.Empty;

        // ✅ TYLKO kombinacja Nazwa2 + " " + Nazwa1
        public string? NormalizedCombined { get; set; }
    }

    /// <summary>
    /// 🚀 Cached wersja Miasto z znormalizowaną nazwą
    /// </summary>
    public class MiastoCached
    {
        public Miasto Miasto { get; set; } = null!;
        public string NormalizedNazwa { get; set; } = string.Empty;
    }
}
