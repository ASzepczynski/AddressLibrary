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
    }
}