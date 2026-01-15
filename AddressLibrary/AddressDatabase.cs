// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Services;
using AddressLibrary.Services.HierarchyBuilders;
using AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Models;

namespace AddressLibrary
{
    public class AddressDatabase
    {
        private readonly string _connectionString;
        private AddressDbContext _context;
        private readonly string? _appDataPath;

        public AddressDatabase(string connectionString, string? appDataPath = null)
        {
            _connectionString = connectionString;
            _appDataPath = appDataPath;
            InitializeContext();
        }

        private void InitializeContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AddressDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            _context = new AddressDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Tworzy bazê danych jeœli nie istnieje
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Usuwa wszystkie dane z wszystkich tabel zachowuj¹c strukturê bazy danych
        /// </summary>
        public async Task DeleteDatabaseAsync()
        {
            // Usuñ dane z tabel hierarchicznych (w odpowiedniej kolejnoœci - od dzieci do rodziców)
            _context.KodyPocztowe.RemoveRange(await _context.KodyPocztowe.ToListAsync());
            _context.Ulice.RemoveRange(await _context.Ulice.ToListAsync());
            _context.Miasta.RemoveRange(await _context.Miasta.ToListAsync());
            _context.Gminy.RemoveRange(await _context.Gminy.ToListAsync());
            _context.Powiaty.RemoveRange(await _context.Powiaty.ToListAsync());
            _context.Wojewodztwa.RemoveRange(await _context.Wojewodztwa.ToListAsync());

            // Usuñ dane ze s³owników
            _context.RodzajeMiast.RemoveRange(await _context.RodzajeMiast.ToListAsync());
            _context.RodzajeGmin.RemoveRange(await _context.RodzajeGmin.ToListAsync());

            // Usuñ dane z tabel TERYT
            _context.Pna.RemoveRange(await _context.Pna.ToListAsync());
            _context.TerytUlic.RemoveRange(await _context.TerytUlic.ToListAsync());
            _context.TerytSimc.RemoveRange(await _context.TerytSimc.ToListAsync());
            _context.TerytTerc.RemoveRange(await _context.TerytTerc.ToListAsync());
            _context.TerytWmRodz.RemoveRange(await _context.TerytWmRodz.ToListAsync());

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Wykonuje migracje
        /// </summary>
        public async Task MigrateDatabaseAsync()
        {
            await _context.Database.MigrateAsync();
        }

        /// <summary>
        /// £aduje dane z pliku CSV do tabeli odpowiadaj¹cej typowi T
        /// </summary>
        /// <typeparam name="T">Typ encji (nazwa tabeli)</typeparam>
        /// <param name="csvFilePath">Œcie¿ka do pliku CSV</param>
        public async Task LoadDataFromCsvAsync<T>(string csvFilePath) where T : class
        {
            var loader = new CsvDataLoader(_context);
            await loader.LoadDataFromCsvAsync<T>(csvFilePath);
        }

        /// <summary>
        /// £aduje dane z pliku PDF do tabeli Pna
        /// </summary>
        /// <param name="pdfFilePath">Œcie¿ka do pliku PDF</param>
        public async Task LoadDataFromPdfAsync(string pdfFilePath)
        {
            var loader = new PdfDataLoader(_context, _appDataPath);
            await loader.LoadDataFromPdfAsync(pdfFilePath);
        }

        /// <summary>
        /// Zwraca DbContext do rêcznych operacji
        /// </summary>
        public AddressDbContext GetContext() => _context;

        /// <summary>
        /// Czyœci wszystkie dane z tabeli typu T
        /// </summary>
        public async Task ClearTableAsync<T>() where T : class
        {
            var dbSet = _context.Set<T>();
            dbSet.RemoveRange(dbSet);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Buduje strukturê hierarchiczn¹ na podstawie danych TERYT (BEZ kodów pocztowych)
        /// </summary>
        public async Task BuildHierarchicalStructureAsync()
        {
            // KROK 1: Wyczyœæ istniej¹ce dane hierarchiczne (oprócz kodów pocztowych)
            await ClearHierarchicalDataAsync();

            // WA¯NE: Wyczyœæ ChangeTracker po operacji DELETE
            _context.ChangeTracker.Clear();

            // KROK 1.5: SEED domyœlnych rekordów "Brak" dla wszystkich tabel
            var seeder = new DefaultRecordSeeder(_context);
            await seeder.SeedDefaultRecordsAsync();

            // Wyczyœæ ChangeTracker ponownie po seedowaniu
            _context.ChangeTracker.Clear();

            // KROK 2: Za³aduj s³owniki referencyjne
            // 2a. Za³aduj rodzaje gmin (seed data)
            var rodzajeGminLoader = new RodzajeGminLoader(_context);
            await rodzajeGminLoader.LoadAsync();

            // 2b. Za³aduj rodzaje miejscowoœci z TerytWmRodz
            var rodzajeMiastaLoader = new RodzajeMiastLoader(_context);
            await rodzajeMiastaLoader.LoadAsync();

            // KROK 3: Za³aduj dane z tabel TERYT
            var tercData = await _context.TerytTerc.ToListAsync();
            var simcData = await _context.TerytSimc.ToListAsync();
            var ulicData = await _context.TerytUlic.ToListAsync();

            // KROK 4: Za³aduj s³owniki do pamiêci
            var rodzajeGmin = await _context.RodzajeGmin.ToDictionaryAsync(r => r.Kod, r => r);
            var rodzajeMiasta = await _context.RodzajeMiast.ToDictionaryAsync(r => r.Kod, r => r);

            // KROK 5: Utwórz województwa (bez seedowania - ju¿ zrobione w kroku 1.5)
            var wojewodztwaLoader = new WojewodztwaLoader(_context);
            var wojewodztwaDict = await wojewodztwaLoader.LoadAsync(tercData);

            // KROK 6: Utwórz powiaty
            var powiatyLoader = new PowiatyLoader(_context);
            var powiatyDict = await powiatyLoader.LoadAsync(tercData, wojewodztwaDict);

            // KROK 7: Utwórz gminy
            var gminyLoader = new GminyLoader(_context, _appDataPath);
            var gminyDict = await gminyLoader.LoadAsync(tercData, powiatyDict, rodzajeGmin);

            // KROK 8: Utwórz miejscowoœci
            var miastaLoader = new MiastaLoader(_context, _appDataPath);
            var miastaDict = await miastaLoader.LoadAsync(simcData, gminyDict, rodzajeMiasta);

            // KROK 9: Utwórz ulice
            var uliceLoader = new UliceLoader(_context, _appDataPath);
            await uliceLoader.LoadAsync(ulicData, miastaDict);
        }

        /// <summary>
        /// £aduje TYLKO kody pocztowe na podstawie danych PNA (wymaga wczeœniejszego wykonania BuildHierarchicalStructureAsync)
        /// </summary>
        /// <param name="progress">Opcjonalny obiekt do raportowania postêpu ³adowania kodów pocztowych</param>
        public async Task LoadKodyPocztoweAsync(IProgress<LoadProgressInfo>? progress = null)
        {
            // Wyczyœæ istniej¹ce kody pocztowe
            var kodyPocztoweToRemove = await _context.KodyPocztowe
                .Where(k => k.Id != -1)
                .ToListAsync();
            _context.KodyPocztowe.RemoveRange(kodyPocztoweToRemove);
            await _context.SaveChangesAsync();

            // Za³aduj dane PNA
            var pnaData = await _context.Pna.ToListAsync();

            // Loader sam za³aduje miejscowoœci i ulice z bazy danych i dopasuje po nazwach
            var kodyPocztoweLoader = new KodyPocztoweLoaderService(_context, _appDataPath); // ZMIENIONO
            await kodyPocztoweLoader.LoadAsync(pnaData, progress);
        }

        /// <summary>
        /// Czyœci istniej¹ce dane hierarchiczne (oprócz rekordów "Brak" z Id=-1)
        /// NIE usuwa kodów pocztowych
        /// </summary>
        private async Task ClearHierarchicalDataAsync()
        {
            // Zwiêksz timeout do 5 minut dla operacji usuwania du¿ych iloœci danych
            var previousTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(300); // 300 sekund = 5 minut

            try
            {
                // U¿ywamy DELETE z wy³¹czonymi constraints
                // WA¯NE: Kolejnoœæ usuwania - od dzieci do rodziców (zgodnie z FK)
                var sql = @"
                    -- Wy³¹cz sprawdzanie kluczy obcych
                    ALTER TABLE KodyPocztowe NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Ulice NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Miasta NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Gminy NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Powiaty NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Wojewodztwa NOCHECK CONSTRAINT ALL;

                    -- Usuñ dane (zachowaj rekordy z Id = -1)
                    -- WA¯NE: KodyPocztowe NAJPIERW (ma FK do Ulice i Miasta)
                    DELETE FROM KodyPocztowe WHERE Id != -1;
                    DELETE FROM Ulice WHERE Id != -1;
                    DELETE FROM Miasta WHERE Id != -1;
                    DELETE FROM Gminy WHERE Id != -1;
                    DELETE FROM Powiaty WHERE Id != -1;
                    DELETE FROM Wojewodztwa WHERE Id != -1;
                    DELETE FROM RodzajeMiast WHERE Id != -1;
                    DELETE FROM RodzajeGmin WHERE Id != -1;

                    -- W³¹cz z powrotem sprawdzanie kluczy obcych
                    ALTER TABLE KodyPocztowe CHECK CONSTRAINT ALL;
                    ALTER TABLE Ulice CHECK CONSTRAINT ALL;
                    ALTER TABLE Miasta CHECK CONSTRAINT ALL;
                    ALTER TABLE Gminy CHECK CONSTRAINT ALL;
                    ALTER TABLE Powiaty CHECK CONSTRAINT ALL;
                    ALTER TABLE Wojewodztwa CHECK CONSTRAINT ALL;
                ";

                await _context.Database.ExecuteSqlRawAsync(sql);
            }
            finally
            {
                // Przywróæ poprzedni timeout
                _context.Database.SetCommandTimeout(previousTimeout);
            }
        }
    }
}