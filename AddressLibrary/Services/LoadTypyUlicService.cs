using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.Dictionaries;
using AddressLibrary.Services.Dictionaries.CechyUlic;
using AddressLibrary.Services.Dictionaries.TytulyStopnie;
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
        private readonly TytulyStopnieDictionary _tytulyDict;
        private readonly CechyUlicDictionary _cechyDict;

        public LoadTypyUlicService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new PostalCodesLogger(appDataPath, "LoadTypyUlic.txt");
            _tytulyDict = new TytulyStopnieDictionary(context);
            _cechyDict = new CechyUlicDictionary(context);
        }

        /// <summary>
        /// Ładuje wszystkie wpisy z TerytUlic względem słownika
        /// </summary>
        public async Task<ValidatorResult> LoadAsync(IProgress<ValidatorProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new ValidatorResult();

            _logger.LogInfo("=== Rozpoczęcie ładowania TypyUlic ===");

            // ✅ KROK 0: Utwórz domyślne rekordy z ID = -1 (KOLEJNOŚĆ MA ZNACZENIE!)
            _logger.LogInfo("KROK 0: Tworzenie domyślnych rekordów z ID = -1...");
            progress?.Report(new ValidatorProgress { CurrentOperation = "Tworzenie domyślnych rekordów..." });

            // WAŻNE: Najpierw TytulyStopnie (tabela nadrzędna)
            await DefaultRecordHelper.EnsureTytulStopienDefaultAsync(_context, _logger);
            
            // Dopiero potem TypyUlic (tabela zależna z kluczem obcym do TytulyStopnie)
            await DefaultRecordHelper.EnsureTypUlicyDefaultAsync(_context, _logger);

            // ✅ KROK 1: Załaduj słownik CechyUlic z Excel
            _logger.LogInfo("KROK 1: Ładowanie słownika CechyUlic z Excel...");
            progress?.Report(new ValidatorProgress { CurrentOperation = "Ładowanie słownika CechyUlic..." });

            var cechyLoader = new CechyUlicExcelLoader(_context, _appDataPath);
            var cechyResult = await cechyLoader.LoadFromExcelAsync(null);

            if (!string.IsNullOrEmpty(cechyResult.ErrorMessage))
            {
                _logger.LogWarning($"Ostrzeżenie przy ładowaniu CechyUlic: {cechyResult.ErrorMessage}");
            }
            else
            {
                _logger.LogInfo($"✓ Załadowano CechyUlic: Dodano={cechyResult.InsertedCount}, Zaktualizowano={cechyResult.UpdatedCount}");
            }

            // Wyczyść cache słownika CechyUlic po ładowaniu
            _cechyDict.ClearCache();

            // ✅ KROK 2: Załaduj słownik TytulyStopnie z Excel
            _logger.LogInfo("KROK 2: Ładowanie słownika TytulyStopnie z Excel...");
            progress?.Report(new ValidatorProgress { CurrentOperation = "Ładowanie słownika TytulyStopnie..." });

            var tytulyLoader = new TytulyStopnieExcelLoader(_context, _appDataPath);
            var tytulyResult = await tytulyLoader.LoadFromExcelAsync(null);

            if (!string.IsNullOrEmpty(tytulyResult.ErrorMessage))
            {
                _logger.LogWarning($"Ostrzeżenie przy ładowaniu TytulyStopnie: {tytulyResult.ErrorMessage}");
            }
            else
            {
                _logger.LogInfo($"✓ Załadowano TytulyStopnie: Dodano={tytulyResult.InsertedCount}, Zaktualizowano={tytulyResult.UpdatedCount}");
            }

            // Wyczyść cache i załaduj ponownie do pamięci
            _tytulyDict.ClearCache();

            // ✅ KROK 3: Załaduj słownik tytułów do pamięci i zainicjalizuj TitleManager
            _logger.LogInfo("KROK 3: Inicjalizacja słownika tytułów w pamięci...");
            await _tytulyDict.GetSkrotToIdMappingAsync();
            await _tytulyDict.GetDopelniaczToIdMappingAsync();

            if (!TitleManager.IsInitialized)
            {
                var tytuly = await _tytulyDict.GetAllAsync();
                TitleManager.Initialize(tytuly);
                _logger.LogInfo($"✓ Zainicjalizowano TitleManager: {tytuly.Count} tytułów");
            }

            // ✅ KROK 4: Wczytaj słownik TypyUlic z Excel
            _logger.LogInfo("KROK 4: Ładowanie słownika TerytUlicPoprawki z Excel...");
            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Ładowanie słownika TypyUlicPoprawki z Excel..."
            });

            var dictionary = TerytUlicPoprawkiDictionary.Load(_appDataPath, _logger);

            if (dictionary.Count == 0)
            {
                _logger.LogError("Słownik TerytUlicPoprawki jest pusty - przerywam ładowanie");
                return result;
            }

            _logger.LogInfo($"✓ Załadowano {dictionary.Count} wpisów ze słownika TerytUlicPoprawki");

            // ✅ KROK 5: Pobierz dane z TerytUlic
            _logger.LogInfo("KROK 5: Pobieranie danych z TerytUlic ...");
            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Pobieranie danych z TerytUlic ..."
            });

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
            
            // ✅ KROK 6: Przetwórz każdy wpis
            _logger.LogInfo("KROK 6: Przetwarzanie wpisów z TerytUlic...");
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
                    uliceList.Add(terytUlicPoprawka);
                }
                else
                {
                    result.NotFoundCount++;
                    _logger.LogWarning($"BRAK w słowniku: '{original}'");
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

            // ✅ KROK 7: Wstaw unikalne wartości do tabeli TypyUlic
            _logger.LogInfo("KROK 7: Wstawianie unikalnych wartości do tabeli TypyUlic...");
            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Wstawianie unikalnych wartości do bazy danych..."
            });

            _logger.LogInfo("=== Rozpoczęcie wstawiania unikalnych wpisów do TypyUlic ===");

            try
            {
                // Usuń wszystkie referencje z tabeli Ulice
                _logger.LogInfo("Usuwanie referencji z tabeli Ulice...");
                await _context.Database.ExecuteSqlRawAsync("UPDATE Ulice SET TypUlicyId = NULL WHERE TypUlicyId IS NOT NULL");
                _logger.LogInfo("✓ Referencje usunięte");

                // Wyczyść tabelę TypyUlic (zachowaj rekord -1)
                _logger.LogInfo("Czyszczenie tabeli TypyUlic...");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM TypyUlic WHERE Id != -1");
                _logger.LogInfo("✓ Tabela wyczyszczona");
                
                // Resetuj licznik IDENTITY
                _logger.LogInfo("Resetowanie licznika IDENTITY...");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('TypyUlic', RESEED, 0)");
                _logger.LogInfo("✓ Licznik zresetowany");

                // Deduplikacja
                _logger.LogInfo($"Deduplikacja {uliceList.Count} wpisów...");
                
                var uniqueUliceSet = new HashSet<TypUlicy>(new TypUlicyEqualityComparer());
                int duplicatesCount = 0;
                int truncatedCount = 0;
                
                foreach (var item in uliceList)
                {
                    // ✅ Użyj słownika do mapowania tytułu
                    int tytulStopienId = _tytulyDict.MapDopelniaczToId(item.Tytul);
                    
                    var typUlicy = new TypUlicy
                    {
                        Prefiks = TruncateString(item.Prefiks, 200, ref truncatedCount, "Prefiks"),
                        TytulStopienId = tytulStopienId,
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

                // Wstaw do bazy danych partiami
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
                        
                        var innerEx = ex.InnerException;
                        int level = 1;
                        while (innerEx != null)
                        {
                            _logger.LogError($"InnerException level {level}: {innerEx.Message}");
                            innerEx = innerEx.InnerException;
                            level++;
                        }
                        
                        _logger.LogError($"Pierwszych 5 rekordów z problematycznej partii {batchNumber}:");
                        for (int j = 0; j < Math.Min(5, batch.Count); j++)
                        {
                            var record = batch[j];
                            _logger.LogError($"  [{j}] Prefiks:'{record.Prefiks}' TytulStopienId:{record.TytulStopienId} Imie:'{record.Imie}' Nazwisko:'{record.Nazwisko}'");
                        }
                        
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

            // KROK 8: Podsumowanie
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
    public class TypUlicyEqualityComparer : IEqualityComparer<TypUlicy>
    {
        public bool Equals(TypUlicy? x, TypUlicy? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return string.Equals(x.Prefiks, y.Prefiks, StringComparison.Ordinal) &&
                   x.TytulStopienId == y.TytulStopienId &&
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
                obj.TytulStopienId,
                obj.Imie ?? "",
                obj.Imie2 ?? "",
                obj.Nazwisko ?? "",
                obj.Nazwisko2 ?? "",
                HashCode.Combine(obj.Pseudonim ?? "", obj.Postfiks ?? "")
            );
        }
    }
}
