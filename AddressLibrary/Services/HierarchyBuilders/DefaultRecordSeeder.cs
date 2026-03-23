// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    /// <summary>
    /// Tworzy domyślne rekordy "Brak" z Id = -1 dla wszystkich encji hierarchicznych
    /// </summary>
    internal class DefaultRecordSeeder
    {
        private readonly AddressDbContext _context;

        public DefaultRecordSeeder(AddressDbContext context)
        {
            _context = context;
        }

        public async Task SeedDefaultRecordsAsync()
        {
            await DefaultRecordHelper.EnsureRodzajGminyDefaultAsync(_context);
            await DefaultRecordHelper.EnsureRodzajMiastaDefaultAsync(_context);
            await DefaultRecordHelper.EnsureCechaUlicyDefaultAsync(_context);
            await DefaultRecordHelper.EnsureTytulStopienDefaultAsync(_context);
            await DefaultRecordHelper.EnsureTypUlicyDefaultAsync(_context);
            await DefaultRecordHelper.EnsureWojewodztwoDefaultAsync(_context);
            await DefaultRecordHelper.EnsurePowiatDefaultAsync(_context);
            await DefaultRecordHelper.EnsureGminaDefaultAsync(_context);
            await DefaultRecordHelper.EnsureMiastoDefaultAsync(_context);
            await DefaultRecordHelper.EnsureUlicaDefaultAsync(_context);  // ✅ DODANO
        }
    }
}