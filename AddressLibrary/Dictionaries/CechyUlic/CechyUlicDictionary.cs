using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Dictionaries.CechyUlic
{
    /// <summary>
    /// Centralny s³ownik dla CechyUlic - zarz¹dzanie cache i dostêp do danych
    /// </summary>
    public class CechyUlicDictionary
    {
        private readonly AddressDbContext _context;
        private Dictionary<string, CechaUlicy>? _nazwaDict;
        private Dictionary<string, CechaUlicy>? _skrotDict;
        private Dictionary<string, int>? _skrotToIdDict;
        private List<CechaUlicy>? _allCechy;

        public CechyUlicDictionary(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Pobiera wszystkie cechy ulic z bazy danych (z cache)
        /// </summary>
        public async Task<List<CechaUlicy>> GetAllAsync()
        {
            if (_allCechy == null)
            {
                _allCechy = await _context.CechyUlic
                    .AsNoTracking()
                    .ToListAsync();
            }
            return _allCechy;
        }

        /// <summary>
        /// Pobiera s³ownik Nazwa -> CechaUlicy
        /// </summary>
        public async Task<Dictionary<string, CechaUlicy>> GetByNazwaAsync()
        {
            if (_nazwaDict == null)
            {
                var cechy = await GetAllAsync();
                _nazwaDict = cechy.ToDictionary(
                    c => c.Nazwa,
                    c => c,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            return _nazwaDict;
        }

        /// <summary>
        /// Pobiera s³ownik Skrot -> CechaUlicy
        /// </summary>
        public async Task<Dictionary<string, CechaUlicy>> GetBySkrotAsync()
        {
            if (_skrotDict == null)
            {
                var cechy = await GetAllAsync();
                _skrotDict = cechy.ToDictionary(
                    c => c.Skrot,
                    c => c,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            return _skrotDict;
        }

        /// <summary>
        /// Pobiera s³ownik Skrot -> Id (dla szybkiego mapowania)
        /// </summary>
        public async Task<Dictionary<string, int>> GetSkrotToIdMappingAsync()
        {
            if (_skrotToIdDict == null)
            {
                var cechy = await GetAllAsync();
                _skrotToIdDict = cechy.ToDictionary(
                    c => c.Skrot,
                    c => c.Id,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            return _skrotToIdDict;
        }

        /// <summary>
        /// Znajduje Id cechy na podstawie skrótu
        /// </summary>
        public async Task<int> FindIdBySkrotAsync(string? skrot)
        {
            if (string.IsNullOrWhiteSpace(skrot))
                return -1;

            var dict = await GetSkrotToIdMappingAsync();
            
            if (dict.TryGetValue(skrot.Trim(), out int id))
                return id;

            return -1;
        }

        /// <summary>
        /// Znajduje cechê na podstawie skrótu
        /// </summary>
        public async Task<CechaUlicy?> FindBySkrotAsync(string? skrot)
        {
            if (string.IsNullOrWhiteSpace(skrot))
                return null;

            var dict = await GetBySkrotAsync();
            
            if (dict.TryGetValue(skrot.Trim(), out var cecha))
                return cecha;

            return null;
        }

        /// <summary>
        /// Czyœci cache
        /// </summary>
        public void ClearCache()
        {
            _nazwaDict = null;
            _skrotDict = null;
            _skrotToIdDict = null;
            _allCechy = null;
        }

        /// <summary>
        /// £aduje dane z bazy danych do statycznej tablicy StreetPrefixes w CechyUlicUtils
        /// Tworzy listê wariantów na podstawie Nazwa i Skrot z ka¿dej CechaUlicy
        /// </summary>
        /// <remarks>
        /// Ta metoda synchronizuje dane z bazy danych do statycznej tablicy StreetPrefixes.
        /// Tworzy warianty:
        /// 1. Skrót (np. "ul.")
        /// 2. Skrót bez kropki (np. "ul")
        /// 3. Pe³na nazwa (np. "ulica")
        /// 
        /// PRZYK£AD:
        /// Dla rekordu: Nazwa="ulica", Skrot="ul."
        /// Zostanie utworzony wpis: StreetPrefixes["ulica"] = ["ul.", "ul", "ulica"]
        /// </remarks>
        public async Task LoadIntoStreetPrefixesAsync()
        {
            // Pobierz wszystkie cechy z bazy
            var cechy = await GetAllAsync();

            // Wyczyœæ istniej¹c¹ tablicê
            CechyUlicUtils.StreetPrefixes.Clear();

            // Dodaj ka¿d¹ cechê do s³ownika
            foreach (var cecha in cechy)
            {
                // Utwórz listê wariantów:
                var warianty = new List<string>();

                // 1. Dodaj skrót (np. "ul.")
                if (!string.IsNullOrWhiteSpace(cecha.Skrot))
                {
                    warianty.Add(cecha.Skrot);

                    // 2. Jeœli skrót koñczy siê kropk¹, dodaj wersjê bez kropki (np. "ul")
                    if (cecha.Skrot.EndsWith("."))
                    {
                        var bezKropki = cecha.Skrot.TrimEnd('.');
                        if (!string.IsNullOrWhiteSpace(bezKropki))
                        {
                            warianty.Add(bezKropki);
                        }
                    }
                }

                // 3. Dodaj pe³n¹ nazwê (np. "ulica")
                if (!string.IsNullOrWhiteSpace(cecha.Nazwa))
                {
                    warianty.Add(cecha.Nazwa);
                }

                // Usuñ duplikaty (case-insensitive)
                warianty = warianty.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Dodaj do s³ownika StreetPrefixes u¿ywaj¹c metody Add()
                // Dictionary.Add() dodaje parê klucz-wartoœæ:
                // - klucz: pe³na nazwa cechy (np. "aleja")
                // - wartoœæ: lista wszystkich wariantów (np. ["al.", "al", "aleja"])
                if (warianty.Count > 0)
                {
                    CechyUlicUtils.Add(cecha.Nazwa, warianty);
                }
            }
        }
    }
}