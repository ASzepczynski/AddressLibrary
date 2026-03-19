using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do ładowania danych TerytUlicPoprawki z Excela do bazy danych
    /// </summary>
    public class LoadTerytUlicPoprawkiService : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly PostalCodesLogger _logger;

        public LoadTerytUlicPoprawkiService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new PostalCodesLogger(appDataPath, "LoadTerytUlicPoprawki.txt");
        }

        /// <summary>
        /// Ładuje dane z Excela do bazy danych
        /// Najpierw ładuje słowniki CechyUlic i TytulyStopnie, potem TerytUlicPoprawki
        /// </summary>
        public async Task<LoadResult> LoadAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();

            _logger.LogInfo("=== Rozpoczęcie ładowania TerytUlicPoprawki ===");

            // KROK 1: Załaduj słownik CechyUlic
            _logger.LogInfo("KROK 1: Ładowanie słownika CechyUlic...");
            progress?.Report(new LoadProgress { CurrentOperation = "Ładowanie słownika CechyUlic..." });

            var cechyLoader = new LoadCechyUlicService(_context, _appDataPath);
            var cechyResult = await cechyLoader.LoadAsync(null);
            
            if (!string.IsNullOrEmpty(cechyResult.ErrorMessage))
            {
                _logger.LogWarning($"Ostrzeżenie przy ładowaniu CechyUlic: {cechyResult.ErrorMessage}");
            }
            else
            {
                _logger.LogInfo($"✓ Załadowano CechyUlic: Dodano={cechyResult.InsertedCount}, Zaktualizowano={cechyResult.UpdatedCount}");
            }

            // KROK 2: Załaduj słownik TytulyStopnie
            _logger.LogInfo("KROK 2: Ładowanie słownika TytulyStopnie...");
            progress?.Report(new LoadProgress { CurrentOperation = "Ładowanie słownika TytulyStopnie..." });

            var tytulyLoader = new LoadTytulyStopnieService(_context, _appDataPath);
            var tytulyResult = await tytulyLoader.LoadAsync(null);
            
            if (!string.IsNullOrEmpty(tytulyResult.ErrorMessage))
            {
                _logger.LogWarning($"Ostrzeżenie przy ładowaniu TytulyStopnie: {tytulyResult.ErrorMessage}");
            }
            else
            {
                _logger.LogInfo($"✓ Załadowano TytulyStopnie: Dodano={tytulyResult.InsertedCount}, Zaktualizowano={tytulyResult.UpdatedCount}");
            }

            // KROK 3: Wczytaj dane TerytUlicPoprawki z Excela
            _logger.LogInfo("KROK 3: Ładowanie TerytUlicPoprawki...");
            progress?.Report(new LoadProgress
            {
                CurrentOperation = "Wczytywanie danych z Excela..."
            });

            var terytUlicDict = TerytUlicPoprawkiDictionary.Load(_appDataPath, _logger);

            if (terytUlicDict.Count == 0)
            {
                _logger.LogError("Brak danych do załadowania - przerywam");
                return result;
            }

            result.TotalCount = terytUlicDict.Count;
            _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excela");

            progress?.Report(new LoadProgress
            {
                CurrentOperation = "Czyszczenie tabeli TerytUlicPoprawki...",
                TotalCount = result.TotalCount
            });

            // KROK 4: Wyczyść tabelę
            _logger.LogInfo("Czyszczenie tabeli TerytUlicPoprawki...");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TerytUlicPoprawki");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('TerytUlicPoprawki', RESEED, 0)");
            _logger.LogInfo("✓ Tabela wyczyszczona");

            progress?.Report(new LoadProgress
            {
                CurrentOperation = "Wstawianie danych do bazy...",
                TotalCount = result.TotalCount
            });

            // KROK 5: Wstaw dane partiami
            _logger.LogInfo("Rozpoczynam wstawianie danych...");

            var dataList = terytUlicDict.Values.ToList();
            const int batchSize = 500;
            int insertedCount = 0;

            for (int i = 0; i < dataList.Count; i += batchSize)
            {
                var batch = dataList.Skip(i).Take(batchSize).ToList();

                _logger.LogInfo($"▶ Wstawiam partię {i / batchSize + 1} ({batch.Count} wpisów)...");

                try
                {
                    await _context.TerytUlicPoprawki.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();

                    // Wyczyść kontekst po każdej paczce
                    _context.ChangeTracker.Clear();

                    insertedCount += batch.Count;

                    _logger.LogInfo($"✓ Wstawiono partię {i / batchSize + 1}: {batch.Count} wpisów (łącznie: {insertedCount}/{dataList.Count})");

                    progress?.Report(new LoadProgress
                    {
                        CurrentOperation = $"Wstawiono {insertedCount}/{dataList.Count} wpisów...",
                        TotalCount = dataList.Count,
                        ProcessedCount = insertedCount
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"⚠️ Błąd podczas wstawiania partii {i / batchSize + 1}: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");
                    
                    // Loguj przykładowe rekordy z problematycznej partii
                    _logger.LogError($"Pierwszych 5 rekordów z problematycznej partii:");
                    for (int j = 0; j < Math.Min(5, batch.Count); j++)
                    {
                        var record = batch[j];
                        _logger.LogError($"  [{j}] Id:'{record.Id}' Cecha:'{record.Cecha}' Nazwisko:'{record.Nazwisko}'");
                    }

                    _context.ChangeTracker.Clear();
                    throw;
                }
            }

            result.InsertedCount = insertedCount;

            // KROK 6: Podsumowanie
            _logger.LogInfo("=== Podsumowanie ładowania ===");
            _logger.LogInfo($"Wczytano z Excela: {result.TotalCount}");
            _logger.LogInfo($"Wstawiono do bazy: {result.InsertedCount}");

            progress?.Report(new LoadProgress
            {
                CurrentOperation = "Zakończono ładowanie",
                TotalCount = result.TotalCount,
                ProcessedCount = result.InsertedCount,
                IsCompleted = true
            });

            return result;
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }

    /// <summary>
    /// Wynik ładowania
    /// </summary>
   
}