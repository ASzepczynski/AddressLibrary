using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Data;

namespace AddressLibrary.Dictionaries.CechyUlic
{
    /// <summary>
    /// Centralny s³ownik dla CechyUlic - zarz¹dzanie cache i dostêp do danych
    /// </summary>
    public class CechyUlicDictionary
    {
        private readonly AddressDbContext _context;
        private Dictionary<string, CechaUlicy>? _nazwaDict;
        private Dictionary<string, List<CechaUlicy>>? _skrotDict;
        private Dictionary<string, List<int>>? _skrotToIdDict;
        private List<CechaUlicy>? _allCechy;

        public CechyUlicDictionary(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Czyœci wszystkie cache'owane s³owniki (wymusza ponowne za³adowanie z bazy danych)
        /// </summary>
        public void ClearCache()
        {
            _nazwaDict = null;
            _skrotDict = null;
            _skrotToIdDict = null;
            _allCechy = null;
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
        /// Pobiera s³ownik Skrot -> Lista CechaUlicy (jeden skrót mo¿e mieæ wiele cech)
        /// </summary>
        public async Task<Dictionary<string, List<CechaUlicy>>> GetBySkrotAsync()
        {
            if (_skrotDict == null)
            {
                var cechy = await GetAllAsync();
                _skrotDict = cechy
                    .GroupBy(c => c.Skrot, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList(),
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            return _skrotDict;
        }

        /// <summary>
        /// Pobiera s³ownik Skrot -> Lista Id (dla szybkiego mapowania, jeden skrót mo¿e mieæ wiele ID)
        /// </summary>
        public async Task<Dictionary<string, List<int>>> GetSkrotToIdMappingAsync()
        {
            if (_skrotToIdDict == null)
            {
                var cechy = await GetAllAsync();
                _skrotToIdDict = cechy
                    .GroupBy(c => c.Skrot, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(c => c.Id).ToList(),
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            return _skrotToIdDict;
        }

        /// <summary>
        /// £aduje cechy ulic z bazy do statycznego s³ownika CechyUlicUtils.StreetPrefixes
        /// </summary>
        public async Task LoadIntoStreetPrefixesAsync()
        {
            var cechy = await GetAllAsync();
            
            // Wyczyœæ istniej¹cy s³ownik
            CechyUlicUtils.StreetPrefixes.Clear();
            
            // Za³aduj dane z bazy do statycznego s³ownika
            // Grupuj po Nazwa (pe³na nazwa cechy), zbierz wszystkie skróty
            var grouped = cechy
                .GroupBy(c => c.Nazwa, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => c.Skrot).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase
                );
            
            foreach (var entry in grouped)
            {
                // Dodaj warianty: [skrót, pe³na nazwa]
                var variants = new List<string>(entry.Value) { entry.Key };
                CechyUlicUtils.Add(entry.Key, variants);
            }
        }

        /// <summary>
        /// Znajduje pierwsze Id cechy na podstawie skrótu (jeœli jest wiele, zwraca pierwsze)
        /// </summary>
        public async Task<int> FindIdBySkrotAsync(string? skrot)
        {
            if (string.IsNullOrWhiteSpace(skrot))
                return -1;

            var dict = await GetSkrotToIdMappingAsync();
            
            if (dict.TryGetValue(skrot.Trim(), out var ids) && ids.Count > 0)
                return ids[0]; // Zwróæ pierwsze ID

            return -1;
        }

        /// <summary>
        /// Znajduje wszystkie Id cech na podstawie skrótu
        /// </summary>
        public async Task<List<int>> FindAllIdsBySkrotAsync(string? skrot)
        {
            if (string.IsNullOrWhiteSpace(skrot))
                return new List<int>();

            var dict = await GetSkrotToIdMappingAsync();
            
            if (dict.TryGetValue(skrot.Trim(), out var ids))
                return ids;

            return new List<int>();
        }

        /// <summary>
        /// Znajduje Id cechy na podstawie pe³nej nazwy (unikalny klucz)
        /// </summary>
        public async Task<int> FindIdByNazwaAsync(string? nazwa)
        {
            if (string.IsNullOrWhiteSpace(nazwa))
                return -1;

            var dict = await GetByNazwaAsync();
            
            if (dict.TryGetValue(nazwa.Trim(), out var cecha))
                return cecha.Id;

            return -1;
        }

        /// <summary>
        /// Znajduje pierwsz¹ cechê na podstawie skrótu
        /// </summary>
        public async Task<CechaUlicy?> FindBySkrotAsync(string? skrot)
        {
            if (string.IsNullOrWhiteSpace(skrot))
                return null;

            var dict = await GetBySkrotAsync();
            
            if (dict.TryGetValue(skrot.Trim(), out var cechy) && cechy.Count > 0)
                return cechy[0];

            return null;
        }

        /// <summary>
        /// Znajduje cechê na podstawie pe³nej nazwy (unikalny klucz)
        /// </summary>
        public async Task<CechaUlicy?> FindByNazwaAsync(string? nazwa)
        {
            if (string.IsNullOrWhiteSpace(nazwa))
                return null;

            var dict = await GetByNazwaAsync();
            
            if (dict.TryGetValue(nazwa.Trim(), out var cecha))
                return cecha;

            return null;
        }
    }
}