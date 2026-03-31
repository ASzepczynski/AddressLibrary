// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Services;
using AddressLibrary.Services.HierarchyBuilders;
using AddressLibrary.Services.KodyPocztoweLoader;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary
{
    public class AddressDatabase
    {
        private readonly string _connectionString;
        private AddressDbContext _context;
        private readonly string? _appDataPath;

        public AddressDatabase(string connectionString, string? appDataPath)
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
        /// Bezpieczna inicjalizacja bazy danych - NIGDY nie kasuje istniejącej bazy automatycznie
        /// - Jeśli baza nie istnieje: tworzy ją
        /// - Jeśli baza istnieje: nic nie robi (nawet jeśli struktura jest błędna)
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // Po prostu upewnij się że baza istnieje
                // EnsureCreatedAsync NIE kasuje istniejącej bazy - tylko tworzy jeśli nie istnieje
                await _context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Błąd podczas inicjalizacji bazy: {ex.Message}");
                throw; // Rzuć wyjątek dalej - nie ignoruj błędów
            }
        }

        /// <summary>
        /// RĘCZNE odtworzenie bazy danych - wymaga świadomej decyzji użytkownika
        /// UWAGA: WSZYSTKIE DANE ZOSTANĄ UTRACONE!
        /// Użyj tego TYLKO gdy chcesz wyczyścić bazę i zacząć od nowa
        /// </summary>
        public async Task ManualRecreateDatabaseAsync()
        {
            Console.WriteLine("⚠️⚠️⚠️ OSTRZEŻENIE: Usuwanie bazy danych...");
            await _context.Database.EnsureDeletedAsync();
            
            Console.WriteLine("✓ Tworzenie bazy od nowa...");
            await _context.Database.EnsureCreatedAsync();
            
            Console.WriteLine("✓ Baza danych została odtworzona");
        }

        /// <summary>
        /// Sprawdza czy baza danych istnieje i można się z nią połączyć
        /// </summary>
        public async Task<bool> CanConnectToDatabaseAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sprawdza czy tabela istnieje w bazie danych
        /// </summary>
        public async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var query = $"SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @p0";
                var result = await _context.Database
                    .SqlQueryRaw<int>(query, tableName)
                    .FirstOrDefaultAsync();
                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tworzy bazę danych jeśli nie istnieje (automatycznie na podstawie modelu)
        /// BEZPIECZNE - nie kasuje istniejącej bazy
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// PRZESTARZAŁE - użyj ManualRecreateDatabaseAsync()
        /// </summary>
        [Obsolete("Użyj ManualRecreateDatabaseAsync() aby wyraźnie zaznaczyć intencję")]
        public async Task RecreateDatabaseAsync()
        {
            await ManualRecreateDatabaseAsync();
        }

        /// <summary>
        /// Usuwa wszystkie dane z wszystkich tabel zachowując strukturę bazy danych
        /// </summary>
        public async Task DeleteDatabaseAsync()
        {
            // Usuń dane z tabel hierarchicznych (w odpowiedniej kolejności - od dzieci do rodziców)
            _context.KodyPocztowe.RemoveRange(await _context.KodyPocztowe.ToListAsync());
            _context.Ulice.RemoveRange(await _context.Ulice.ToListAsync());
            _context.Miasta.RemoveRange(await _context.Miasta.ToListAsync());
            _context.Gminy.RemoveRange(await _context.Gminy.ToListAsync());
            _context.Powiaty.RemoveRange(await _context.Powiaty.ToListAsync());
            _context.Wojewodztwa.RemoveRange(await _context.Wojewodztwa.ToListAsync());

            // Usuń dane ze słowników
            _context.RodzajeMiast.RemoveRange(await _context.RodzajeMiast.ToListAsync());
            _context.RodzajeGmin.RemoveRange(await _context.RodzajeGmin.ToListAsync());

            // Usuń dane z tabel TERYT
            _context.Pna.RemoveRange(await _context.Pna.ToListAsync());
            _context.TerytUlic.RemoveRange(await _context.TerytUlic.ToListAsync());
            _context.TerytSimc.RemoveRange(await _context.TerytSimc.ToListAsync());
            _context.TerytTerc.RemoveRange(await _context.TerytTerc.ToListAsync());
            _context.TerytWmRodz.RemoveRange(await _context.TerytWmRodz.ToListAsync());

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Ładuje dane z pliku CSV do tabeli odpowiadającej typowi T
        /// </summary>
        /// <typeparam name="T">Typ encji (nazwa tabeli)</typeparam>
        /// <param name="csvFilePath">Ścieżka do pliku CSV</param>
        public async Task LoadDataFromCsvAsync<T>(string csvFilePath) where T : class
        {
            var loader = new CsvDataLoader(_context);
            await loader.LoadDataFromCsvAsync<T>(csvFilePath);
        }

        /// <summary>
        /// Ładuje dane z pliku PDF do tabeli Pna
        /// </summary>
        /// <param name="pdfFilePath">Ścieżka do pliku PDF</param>
        public async Task LoadDataFromPdfAsync(string pdfFilePath)
        {
            var loader = new PdfDataLoader(_context, _appDataPath);
            await loader.LoadDataFromPdfAsync(pdfFilePath);
        }

        /// <summary>
        /// Zwraca DbContext do ręcznych operacji
        /// </summary>
        public AddressDbContext GetContext() => _context;

        /// <summary>
        /// Czyści wszystkie dane z tabeli typu T
        /// </summary>
        public async Task ClearTableAsync<T>() where T : class
        {
            var dbSet = _context.Set<T>();
            dbSet.RemoveRange(dbSet);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Buduje strukturę hierarchiczną na podstawie danych TERYT (BEZ kodów pocztowych)
        /// </summary>
        /// <param name="progress">Opcjonalny obiekt do raportowania postępu budowania hierarchii</param>
        public async Task BuildHierarchicalStructureAsync(IProgress<BuildProgressInfo>? progress = null)
        {
            progress?.Report(new BuildProgressInfo(0, 9, "Czyszczenie istniejących danych..."));
            
            // KROK 1: Wyczyść istniejące dane hierarchiczne (oprócz kodów pocztowych)
            await ClearHierarchicalDataAsync();

            // WAŻNE: Wyczyść ChangeTracker po operacji DELETE
            _context.ChangeTracker.Clear();

            progress?.Report(new BuildProgressInfo(1, 9, "Seedowanie domyślnych rekordów..."));
            
            // KROK 1.5: SEED domyślnych rekordów "Brak" dla wszystkich tabel
            var seeder = new DefaultRecordSeeder(_context);
            await seeder.SeedDefaultRecordsAsync();

            // Wyczyść ChangeTracker ponownie po seedowaniu
            _context.ChangeTracker.Clear();

            progress?.Report(new BuildProgressInfo(2, 9, "Ładowanie słowników (rodzaje gmin)..."));
            
            // KROK 2: Załaduj słowniki referencyjne
            // 2a. Załaduj rodzaje gmin (seed data)
            var rodzajeGminLoader = new RodzajeGminLoader(_context);
            await rodzajeGminLoader.LoadAsync();

            progress?.Report(new BuildProgressInfo(3, 9, "Ładowanie słowników (rodzaje miejscowości)..."));
            
            // 2b. Załaduj rodzaje miejscowości z TerytWmRodz
            var rodzajeMiastaLoader = new RodzajeMiastLoader(_context);
            await rodzajeMiastaLoader.LoadAsync();

            progress?.Report(new BuildProgressInfo(4, 9, "Wczytywanie danych TERYT..."));
            
            // KROK 3: Załaduj dane z tabel TERYT
            var tercData = await _context.TerytTerc.ToListAsync();
            var simcData = await _context.TerytSimc.ToListAsync();
            var ulicData = await _context.TerytUlic.ToListAsync();

            // KROK 4: Załaduj słowniki do pamięci
            var rodzajeGmin = await _context.RodzajeGmin.ToDictionaryAsync(r => r.Kod, r => r);
            var rodzajeMiasta = await _context.RodzajeMiast.ToDictionaryAsync(r => r.Kod, r => r);

            progress?.Report(new BuildProgressInfo(5, 9, "Tworzenie województw..."));
            
            // KROK 5: Utwórz województwa (bez seedowania - już zrobione w kroku 1.5)
            var wojewodztwaLoader = new WojewodztwaLoader(_context);
            var wojewodztwaDict = await wojewodztwaLoader.LoadAsync(tercData);

            progress?.Report(new BuildProgressInfo(6, 9, "Tworzenie powiatów..."));
            
            // KROK 6: Utwórz powiaty
            var powiatyLoader = new PowiatyLoader(_context);
            var powiatyDict = await powiatyLoader.LoadAsync(tercData, wojewodztwaDict);

            progress?.Report(new BuildProgressInfo(7, 9, "Tworzenie gmin..."));
            
            // KROK 7: Utwórz gminy
            var gminyLoader = new GminyLoader(_context, _appDataPath);
            var gminyDict = await gminyLoader.LoadAsync(tercData, powiatyDict, rodzajeGmin);

            progress?.Report(new BuildProgressInfo(8, 9, "Tworzenie miejscowości..."));
            
            // KROK 8: Utwórz miejscowości
            var miastaLoader = new MiastaLoader(_context, _appDataPath);
            var miastaDict = await miastaLoader.LoadAsync(simcData, gminyDict, rodzajeMiasta);
            miastaLoader.Dispose();

            progress?.Report(new BuildProgressInfo(9, 9, "Tworzenie ulic..."));
            
            // KROK 9: Utwórz ulice
            var uliceLoader = new UliceLoader(_context, _appDataPath);
            
            await uliceLoader.LoadAsync(ulicData, miastaDict, _appDataPath);
            uliceLoader.Dispose();
            
            progress?.Report(new BuildProgressInfo(9, 9, "✅ Budowanie hierarchii zakończone!"));
        }

        /// <summary>
        /// Ładuje TYLKO kody pocztowe na podstawie danych PNA (wymaga wcześniejszego wykonania BuildHierarchicalStructureAsync)
        /// </summary>
        /// <param name="progress">Opcjonalny obiekt do raportowania postępu ładowania kodów pocztowych</param>
        public async Task LoadKodyPocztoweAsync(IProgress<LoadProgressInfo>? progress = null)
        {
            // Wyczyść istniejące kody pocztowe
            var kodyPocztoweToRemove = await _context.KodyPocztowe
                .Where(k => k.Id != -1)
                .ToListAsync();
            _context.KodyPocztowe.RemoveRange(kodyPocztoweToRemove);
            await _context.SaveChangesAsync();

            // Załaduj dane PNA
            var pnaData = await _context.Pna.ToListAsync();

            // Loader sam załaduje miejscowości i ulice z bazy danych i dopasuje po nazwach
            var kodyPocztoweLoader = new KodyPocztoweLoaderService(_context, _appDataPath); // ZMIENIONO
            await kodyPocztoweLoader.LoadAsync(pnaData, progress);
        }

        /// <summary>
        /// Czyści istniejące dane hierarchiczne (oprócz rekordów "Brak" z Id=-1)
        /// NIE usuwa kodów pocztowych
        /// </summary>
        private async Task ClearHierarchicalDataAsync()
        {
            // Zwiększ timeout do 5 minut dla operacji usuwania dużych ilości danych
            var previousTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(300); // 300 sekund = 5 minut

            try
            {
                // Używamy DELETE z wyłączonymi constraints
                // WAŻNE: Kolejność usuwania - od dzieci do rodziców (zgodnie z FK)
                var sql = @"
                    -- Wyłącz sprawdzanie kluczy obcych
                    ALTER TABLE KodyPocztowe NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Ulice NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Miasta NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Gminy NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Powiaty NOCHECK CONSTRAINT ALL;
                    ALTER TABLE Wojewodztwa NOCHECK CONSTRAINT ALL;

                    -- Usuń dane (zachowaj rekordy z Id = -1)
                    -- WAŻNE: KodyPocztowe NAJPIERW (ma FK do Ulice i Miasta)
                    DELETE FROM KodyPocztowe WHERE Id != -1;
                    DELETE FROM Ulice WHERE Id != -1;
                    DELETE FROM Miasta WHERE Id != -1;
                    DELETE FROM Gminy WHERE Id != -1;
                    DELETE FROM Powiaty WHERE Id != -1;
                    DELETE FROM Wojewodztwa WHERE Id != -1;
                    DELETE FROM RodzajeMiast WHERE Id != -1;
                    DELETE FROM RodzajeGmin WHERE Id != -1;

                    -- Włącz z powrotem sprawdzanie kluczy obcych
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
                // Przywróć poprzedni timeout
                _context.Database.SetCommandTimeout(previousTimeout);
            }
        }
    }
}