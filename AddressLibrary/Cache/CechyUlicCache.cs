using AddressLibrary.Data;
using AddressLibrary.Dictionaries.CechyUlic;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Cache cech ulic — inicjalizuje statyczny s³ownik CechyUlicUtils z bazy danych.
    /// </summary>
    public class CechyUlicCache
    {
        private readonly AddressDbContext _context;

        public bool IsInitialized => CechyUlicUtils.IsInitialized;

        public CechyUlicCache(AddressDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            if (CechyUlicUtils.IsInitialized)
                return;

            var cechy = await _context.CechyUlic
                .AsNoTracking()
                .Where(c => c.Id != -1)
                .ToListAsync();

            foreach (var cecha in cechy)
            {
                var warianty = new List<string> { cecha.Skrot };
                if (!cecha.Skrot.Equals(cecha.Nazwa, StringComparison.OrdinalIgnoreCase))
                    warianty.Add(cecha.Nazwa);
                CechyUlicUtils.Add(cecha.Nazwa, warianty);
            }
        }

        public void Invalidate() => CechyUlicUtils.StreetPrefixes.Clear();
    }
}
