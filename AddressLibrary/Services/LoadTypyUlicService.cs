using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do ładowania nazw ulic z TerytUlic względem słownika TypyUlic.xlsx
    /// </summary>
    public class LoadTypyUlicService : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly PostalCodesLogger _logger;

        public LoadTypyUlicService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new PostalCodesLogger(appDataPath, "LoadTypyUlic.txt");
        }

        /// <summary>
        /// Ładuje wszystkie wpisy z TerytUlic względem słownika
        /// </summary>
        public async Task<ValidatorResult> LoadAsync(IProgress<ValidatorProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new ValidatorResult();

            _logger.LogInfo("=== Rozpoczęcie ładowania TypyUlic ===");

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Ładowanie słownika TypyUlic..."
            });

            // KROK 1: Wczytaj słownik
            var dictionary = TerytUlicPoprawkiDictionary.Load(_appDataPath,_logger);

            if (dictionary.Count == 0)
            {
                _logger.LogError("Słownik jest pusty - przerywam ładowanie");
                return result;
            }

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Pobieranie danych z TerytUlic..."
            });

            // KROK 2: Pobierz wszystkie wpisy z TerytUlic
            var terytUlice = await _context.TerytUlic
                .Where(u => !string.IsNullOrEmpty(u.Nazwa1))
                .ToListAsync();

            result.TotalCount = terytUlice.Count;

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = $"Przetwarzanie {result.TotalCount} wpisów...",
                TotalCount = result.TotalCount
            });


            var uliceList = new List<TerytUlicPoprawka>();
            // KROK 3: Przetwórz każdy wpis
            foreach (var terytUlica in terytUlice)
            {
                result.ProcessedCount++;

                // Zbuduj klucz: Cecha + Nazwa2 + Nazwa1
                var originalParts = new List<string>();

                if (!string.IsNullOrWhiteSpace(terytUlica.Cecha))
                    originalParts.Add(terytUlica.Cecha.Trim());

                if (!string.IsNullOrWhiteSpace(terytUlica.Nazwa2))
                    originalParts.Add(terytUlica.Nazwa2.Trim());

                if (!string.IsNullOrWhiteSpace(terytUlica.Nazwa1))
                    originalParts.Add(terytUlica.Nazwa1.Trim());

                var original = string.Join(" ", originalParts);

                // Sprawdź czy wpis istnieje w słowniku
                if (dictionary.TryGetValue(original, out var terytUlicPoprawka))
                {
                    result.FoundCount++;

                    // ✅ Porównaj terytUlica ze znalezioną pozycją w słowniku

                    uliceList.Add(terytUlicPoprawka);
                }
                else
                {
                    result.NotFoundCount++;

                    // Loguj brakujący wpis
                    _logger.LogWarning(
                        $"BRAK w słowniku: '{original}'"
                    );
                }

                // Raportuj postęp co 1000 wpisów
                if (result.ProcessedCount % 1000 == 0 || result.ProcessedCount == result.TotalCount)
                {
                    progress?.Report(new ValidatorProgress
                    {
                        CurrentOperation = $"Przetw: {result.ProcessedCount}/{result.TotalCount} | Znaleziono: {result.FoundCount} | Brak: {result.NotFoundCount}",
                        TotalCount = result.TotalCount,
                        ProcessedCount = result.ProcessedCount
                    });
                }
            }

            // KROK 4: Wstaw unikalne wartości do tabeli TypyUlic
            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Wstawianie unikalnych wartości do bazy danych..."
            });

            _logger.LogInfo("=== Rozpoczęcie wstawiania unikalnych wpisów do TypyUlic ===");

            // ✅ Usuń wszystkie referencje z tabeli Ulice
            _logger.LogInfo("Usuwanie referencji z tabeli Ulice...");
            await _context.Database.ExecuteSqlRawAsync("UPDATE Ulice SET TypUlicyId = NULL WHERE TypUlicyId IS NOT NULL");

            // ✅ Wyczyść tabelę TypyUlic (DELETE zamiast TRUNCATE - działa z kluczami obcymi)
            _logger.LogInfo("Czyszczenie tabeli TypyUlic...");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TypyUlic");
            
            // ✅ Opcjonalnie: Zresetuj licznik IDENTITY do 1
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('TypyUlic', RESEED, 0)");

            // Grupuj według wszystkich pól oprócz Id, aby znaleźć unikalne wpisy
            var uniqueUlice = uliceList
                .GroupBy(u => new
                {
                    u.Prefiks,
                    u.Tytul,
                    u.Imie,
                    u.Imie2,
                    u.Nazwisko,
                    u.Nazwisko2,
                    u.Postfiks
                })
                .Select(g => new TypUlicy
                {
                    Prefiks = g.Key.Prefiks,
                    Tytul = g.Key.Tytul,
                    Imie = g.Key.Imie,
                    Imie2 = g.Key.Imie2,
                    Nazwisko = g.Key.Nazwisko,
                    Nazwisko2 = g.Key.Nazwisko2,
                    Postfiks = g.Key.Postfiks
                })
                .ToList();

            _logger.LogInfo($"Znaleziono {uniqueUlice.Count} unikalnych wpisów z {uliceList.Count} wszystkich");

            // Wstaw do bazy danych partiami (np. 1000 naraz)
            const int batchSize = 1000;
            int insertedCount = 0;

            for (int i = 0; i < uniqueUlice.Count; i += batchSize)
            {
                var batch = uniqueUlice.Skip(i).Take(batchSize).ToList();
                await _context.TypyUlic.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                
                insertedCount += batch.Count;
                
                progress?.Report(new ValidatorProgress
                {
                    CurrentOperation = $"Wstawiono {insertedCount}/{uniqueUlice.Count} unikalnych wpisów...",
                    TotalCount = uniqueUlice.Count,
                    ProcessedCount = insertedCount
                });

                _logger.LogInfo($"Wstawiono partię {i / batchSize + 1}: {batch.Count} wpisów (łącznie: {insertedCount})");
            }

            _logger.LogInfo($"✓ Zakończono wstawianie: {insertedCount} unikalnych wpisów");

            // KROK 5: Podsumowanie
            _logger.LogInfo("=== Podsumowanie ładowania ===");
            _logger.LogInfo($"Przetworzono: {result.ProcessedCount}");
            _logger.LogInfo($"Znaleziono w słowniku: {result.FoundCount}");
            _logger.LogInfo($"Brak w słowniku: {result.NotFoundCount}");
            _logger.LogInfo($"Unikalnych wpisów wstawionych: {insertedCount}");
            _logger.LogInfo($"Procent pokrycia: {(result.FoundCount * 100.0 / result.TotalCount):F2}%");

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Zakończono ładowanie",
                TotalCount = result.TotalCount,
                ProcessedCount = result.ProcessedCount,
                IsCompleted = true
            });

            return result;
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
    
}
