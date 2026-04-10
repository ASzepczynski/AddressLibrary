using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Cache tytu³ów stopni — inicjalizuje statyczny TitleManager z bazy danych.
    /// </summary>
    public class TytulyStopnieCache
    {
        private readonly AddressDbContext _context;

        public bool IsInitialized => TitleManager.IsInitialized;

        public TytulyStopnieCache(AddressDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            if (TitleManager.IsInitialized)
                return;

            var tytu³y = await _context.TytulyStopnie
                .AsNoTracking()
                .Where(t => t.Id != -1)
                .ToListAsync();

            TitleManager.Initialize(tytu³y);
        }

        public void Invalidate() => TitleManager.Reset();
    }
}
