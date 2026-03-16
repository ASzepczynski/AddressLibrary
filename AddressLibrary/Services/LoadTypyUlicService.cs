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

            try
            {
                // ✅ Usuń wszystkie referencje z tabeli Ulice
                _logger.LogInfo("Usuwanie referencji z tabeli Ulice...");
                await _context.Database.ExecuteSqlRawAsync("UPDATE Ulice SET TypUlicyId = NULL WHERE TypUlicyId IS NOT NULL");
                _logger.LogInfo("✓ Referencje usunięte");

                // ✅ Wyczyść tabelę TypyUlic (DELETE zamiast TRUNCATE - działa z kluczami obcymi)
                _logger.LogInfo("Czyszczenie tabeli TypyUlic...");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM TypyUlic");
                _logger.LogInfo("✓ Tabela wyczyszczona");
                
                // ✅ Opcjonalnie: Zresetuj licznik IDENTITY do 1
                _logger.LogInfo("Resetowanie licznika IDENTITY...");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('TypyUlic', RESEED, 0)");
                _logger.LogInfo("✓ Licznik zresetowany");

                // ✅ OPTYMALIZACJA: Użyj HashSet zamiast GroupBy dla szybszej deduplikacji
                _logger.LogInfo($"Deduplikacja {uliceList.Count} wpisów...");
                
                var uniqueUliceSet = new HashSet<TypUlicy>(new TypUlicyEqualityComparer());
                int duplicatesCount = 0;
                int truncatedCount = 0;
                
                foreach (var item in uliceList)
                {
                    var typUlicy = new TypUlicy
                    {
                        Prefiks = TruncateString(item.Prefiks, 200, ref truncatedCount, "Prefiks"),
                        Tytul = TruncateString(item.Tytul, 100, ref truncatedCount, "Tytul"),
                        Imie = TruncateString(item.Imie, 200, ref truncatedCount, "Imie"),
                        Imie2 = TruncateString(item.Imie2, 200, ref truncatedCount, "Imie2"),
                        Nazwisko = TruncateString(item.Nazwisko, 200, ref truncatedCount, "Nazwisko"),
                        Nazwisko2 = TruncateString(item.Nazwisko2, 200, ref truncatedCount, "Nazwisko2"),
                        Pseudonim = TruncateString(item.Pseudonim, 200, ref truncatedCount, "Pseudonim"),
                        Postfiks = TruncateString(item.Postfiks, 200, ref truncatedCount, "Postfiks")
                    };
                    
                    if (!uniqueUliceSet.Add(typUlicy))
                    {
                        duplicatesCount++;
                    }
                }
                
                var uniqueUlice = uniqueUliceSet.ToList();
                
                if (truncatedCount > 0)
                {
                    _logger.LogWarning($"⚠️ Przycięto {truncatedCount} wartości przekraczających limity");
                }
                
                _logger.LogInfo($"Znaleziono {uniqueUlice.Count} unikalnych wpisów (pominięto {duplicatesCount} duplikatów z {uliceList.Count} wszystkich)");

                // Wstaw do bazy danych partiami (np. 500 naraz - zmniejszono dla stabilności)
                const int batchSize = 500;
                int insertedCount = 0;
                int batchNumber = 0;

                for (int i = 0; i < uniqueUlice.Count; i += batchSize)
                {
                    batchNumber++;
                    var batch = uniqueUlice.Skip(i).Take(batchSize).ToList();
                    
                    _logger.LogInfo($"▶ Rozpoczynam wstawianie partii {batchNumber} ({batch.Count} wpisów)...");
                    
                    try
                    {
                        await _context.TypyUlic.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        
                        // ✅ Wyczyść kontekst po każdej paczce, aby uniknąć problemów z pamięcią
                        _context.ChangeTracker.Clear();
                        
                        insertedCount += batch.Count;
                        
                        _logger.LogInfo($"✓ Wstawiono partię {batchNumber}: {batch.Count} wpisów (łącznie: {insertedCount}/{uniqueUlice.Count})");
                        
                        progress?.Report(new ValidatorProgress
                        {
                            CurrentOperation = $"Wstawiono {insertedCount}/{uniqueUlice.Count} unikalnych wpisów (partia {batchNumber})...",
                            TotalCount = uniqueUlice.Count,
                            ProcessedCount = insertedCount
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"⚠️ Błąd podczas wstawiania partii {batchNumber}: {ex.Message}");
                        
                        // ✅ DODANO: Logowanie wszystkich wyjątków wewnętrznych
                        var innerEx = ex.InnerException;
                        int level = 1;
                        while (innerEx != null)
                        {
                            _logger.LogError($"InnerException level {level}: {innerEx.Message}");
                            _logger.LogError($"InnerException StackTrace: {innerEx.StackTrace}");
                            innerEx = innerEx.InnerException;
                            level++;
                        }
                        
                        // ✅ DODANO: Logowanie problematycznych rekordów z partii
                        _logger.LogError($"Pierwszych 5 rekordów z problematycznej partii {batchNumber}:");
                        for (int j = 0; j < Math.Min(5, batch.Count); j++)
                        {
                            var record = batch[j];
                            _logger.LogError($"  [{j}] Prefiks:'{record.Prefiks}' Tytul:'{record.Tytul}' Imie:'{record.Imie}' Imie2:'{record.Imie2}' Nazwisko:'{record.Nazwisko}' Nazwisko2:'{record.Nazwisko2}' Postfiks:'{record.Postfiks}'");
                        }
                        
                        _logger.LogError($"Stack trace: {ex.StackTrace}");
                        
                        // ✅ Wyczyść kontekst przed rzuceniem wyjątku
                        _context.ChangeTracker.Clear();
                        
                        throw;
                    }
                }

                _logger.LogInfo($"✓ Zakończono wstawianie: {insertedCount} unikalnych wpisów");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Krytyczny błąd podczas wstawiania do TypyUlic: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }

            // KROK 5: Podsumowanie
            _logger.LogInfo("=== Podsumowanie ładowania ===");
            _logger.LogInfo($"Przetworzono: {result.ProcessedCount}");
            _logger.LogInfo($"Znaleziono w słowniku: {result.FoundCount}");
            _logger.LogInfo($"Brak w słowniku: {result.NotFoundCount}");
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

        /// <summary>
        /// Przycina string do maksymalnej długości i loguje ostrzeżenie
        /// </summary>
        private string? TruncateString(string? value, int maxLength, ref int truncatedCount, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= maxLength)
                return value;

            truncatedCount++;
            _logger.LogWarning($"⚠️ Przycięto {fieldName}: '{value}' (długość: {value.Length}) do {maxLength} znaków");
            return value.Substring(0, maxLength);
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }

    /// <summary>
    /// Comparer do porównywania TypUlicy pod kątem unikalności (bez Id)
    /// </summary>
    internal class TypUlicyEqualityComparer : IEqualityComparer<TypUlicy>
    {
        public bool Equals(TypUlicy? x, TypUlicy? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return string.Equals(x.Prefiks, y.Prefiks, StringComparison.Ordinal) &&
                   string.Equals(x.Tytul, y.Tytul, StringComparison.Ordinal) &&
                   string.Equals(x.Imie, y.Imie, StringComparison.Ordinal) &&
                   string.Equals(x.Imie2, y.Imie2, StringComparison.Ordinal) &&
                   string.Equals(x.Nazwisko, y.Nazwisko, StringComparison.Ordinal) &&
                   string.Equals(x.Nazwisko2, y.Nazwisko2, StringComparison.Ordinal) &&
                   string.Equals(x.Pseudonim, y.Pseudonim, StringComparison.Ordinal) &&
                   string.Equals(x.Postfiks, y.Postfiks, StringComparison.Ordinal);
        }

        public int GetHashCode(TypUlicy obj)
        {
            if (obj is null) return 0;

            return HashCode.Combine(
                obj.Prefiks ?? "",
                obj.Tytul ?? "",
                obj.Imie ?? "",
                obj.Imie2 ?? "",
                obj.Nazwisko ?? "",
                obj.Nazwisko2 ?? "",
                HashCode.Combine(obj.Pseudonim ?? "", obj.Postfiks ?? "")
            );
        }
    }
}
