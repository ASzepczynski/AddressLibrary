// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    /// <summary>
    /// Tworzy domyœlne rekordy "Brak" z Id = -1 dla wszystkich encji hierarchicznych
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
            // 1. Województwo "Brak"
            await SeedWojewodztwoAsync();

            // 2. Powiat "Brak"
            await SeedPowiatAsync();

            // 3. RodzajGminy "Brak"
            await SeedRodzajGminyAsync();

            // 4. Gmina "Brak"
            await SeedGminaAsync();

            // 5. RodzajMiasta "Brak"
            await SeedRodzajMiastaAsync();

            // 6. Miasto "Brak"
            await SeedMiastoAsync();

            // 7. Ulica "Brak"
            await SeedUlicaAsync();

            // 8. KodPocztowy "Brak"
            await SeedKodPocztowyAsync();
        }

        private async Task SeedWojewodztwoAsync()
        {
            if (!await _context.Wojewodztwa.AnyAsync(w => w.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT Wojewodztwa ON;
                    INSERT INTO Wojewodztwa (Id, Kod, Nazwa) VALUES (-1, '00', 'Brak');
                    SET IDENTITY_INSERT Wojewodztwa OFF;
                    DBCC CHECKIDENT ('Wojewodztwa', RESEED, 0);
                ");
            }
        }

        private async Task SeedPowiatAsync()
        {
            if (!await _context.Powiaty.AnyAsync(p => p.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT Powiaty ON;
                    INSERT INTO Powiaty (Id, Kod, Nazwa, WojewodztwoId) VALUES (-1, '0000', 'Brak', -1);
                    SET IDENTITY_INSERT Powiaty OFF;
                    DBCC CHECKIDENT ('Powiaty', RESEED, 0);
                ");
            }
        }

        private async Task SeedRodzajGminyAsync()
        {
            if (!await _context.RodzajeGmin.AnyAsync(rg => rg.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT RodzajeGmin ON;
                    INSERT INTO RodzajeGmin (Id, Kod, Nazwa) VALUES (-1, '0', 'Brak');
                    SET IDENTITY_INSERT RodzajeGmin OFF;
                    DBCC CHECKIDENT ('RodzajeGmin', RESEED, 0);
                ");
            }
        }

        private async Task SeedGminaAsync()
        {
            if (!await _context.Gminy.AnyAsync(g => g.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT Gminy ON;
                    INSERT INTO Gminy (Id, Kod, Nazwa, PowiatId, RodzajGminyId) VALUES (-1, '000000', 'Brak', -1, -1);
                    SET IDENTITY_INSERT Gminy OFF;
                    DBCC CHECKIDENT ('Gminy', RESEED, 0);
                ");
            }
        }

        private async Task SeedRodzajMiastaAsync()
        {
            if (!await _context.RodzajeMiast.AnyAsync(rm => rm.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT RodzajeMiast ON;
                    INSERT INTO RodzajeMiast (Id, Kod, Nazwa) VALUES (-1, '--', 'Brak');
                    SET IDENTITY_INSERT RodzajeMiast OFF;
                    DBCC CHECKIDENT ('RodzajeMiast', RESEED, 0);
                ");
            }
        }

        private async Task SeedMiastoAsync()
        {
            if (!await _context.Miasta.AnyAsync(m => m.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT Miasta ON;
                    INSERT INTO Miasta (Id, Symbol, Nazwa, GminaId, RodzajMiastaId) 
                    VALUES (-1, '0000000', 'Brak', -1, -1);
                    SET IDENTITY_INSERT Miasta OFF;
                    DBCC CHECKIDENT ('Miasta', RESEED, 0);
                ");
            }
        }

        private async Task SeedUlicaAsync()
        {
            if (!await _context.Ulice.AnyAsync(u => u.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT Ulice ON;
                    INSERT INTO Ulice (Id, Symbol, Nazwa1, Nazwa2, MiastoId) 
                    VALUES (-1, '00000', 'Brak', '', -1);
                    SET IDENTITY_INSERT Ulice OFF;
                    DBCC CHECKIDENT ('Ulice', RESEED, 0);
                ");
            }
        }

        private async Task SeedKodPocztowyAsync()
        {
            if (!await _context.KodyPocztowe.AnyAsync(k => k.Id == -1))
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT KodyPocztowe ON;
                    INSERT INTO KodyPocztowe (Id, Kod, Numery, MiastoId, UlicaId) 
                    VALUES (-1, '00-000', '', -1, -1);
                    SET IDENTITY_INSERT KodyPocztowe OFF;
                    DBCC CHECKIDENT ('KodyPocztowe', RESEED, 0);
                ");
            }
        }
    }
}