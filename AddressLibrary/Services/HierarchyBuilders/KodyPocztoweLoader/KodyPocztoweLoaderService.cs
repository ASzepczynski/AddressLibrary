using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Ładuje kody pocztowe z tablicy PNA do struktury hierarchicznej.
    /// </summary>
    public class KodyPocztoweLoaderService
    {
        private readonly AddressDbContext _context;
        private readonly LoadLogger _logger;

        public string LogFilePath => _logger.LogFilePath;

        public KodyPocztoweLoaderService(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new LoadLogger(appDataPath);
        }

        public async Task LoadAsync(
            List<Pna> pnaData,
            IProgress<LoadProgressInfo>? progress = null)
        {
            await _logger.InitializeAsync();

            // DODANO: Wyczyść tabelę KodyPocztowe przed rozpoczęciem ładowania
            var progressInfo = new LoadProgressInfo
            {
                TotalCount = pnaData.Count,
                CurrentOperation = "Czyszczenie tabeli KodyPocztowe..."
            };
            progress?.Report(progressInfo);

            _logger.LogError("=== Rozpoczęcie czyszczenia tabeli KodyPocztowe ===");
            
            try
            {
                // Usuń wszystkie rekordy z tabeli KodyPocztowe
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM KodyPocztowe");
                _logger.LogError("✓ Tabela KodyPocztowe została wyczyszczona");
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Błąd podczas czyszczenia tabeli: {ex.Message}");
                throw;
            }

            await _logger.FlushAsync();

            progressInfo.CurrentOperation = "Ładowanie danych hierarchicznych...";
            progress?.Report(progressInfo);

            // Buduj słowniki
            var dictionaryBuilder = new KodyPocztoweDictionaryBuilder(_context);
            var gminyDict = await dictionaryBuilder.BuildGminyDictionaryAsync();
            var miejscowosciDict = await dictionaryBuilder.BuildMiejscowosciDictionaryAsync();
            var uliceDict = await dictionaryBuilder.BuildUliceDictionaryAsync();

            // Inicjalizuj matchery - PRZEKAŻ LOGGER
            var miejscowoscMatcher = new MiejscowoscMatcher(gminyDict, miejscowosciDict, _logger);
            var ulicaMatcher = new UlicaMatcher(uliceDict);
            var recordBuilder = new KodPocztowyRecordBuilder();

            progressInfo.CurrentOperation = "Przetwarzanie kodów pocztowych...";
            progress?.Report(progressInfo);

            var stats = new LoadStatistics();
            var pendingRecords = new List<KodPocztowy>(1000);
            const int batchSize = 1000;
            const int reportInterval = 500;
            const int logFlushInterval = 100;

            foreach (var pna in pnaData)
            {
                try
                {
                    // 1. Znajdź miejscowość
                    var matchResult = miejscowoscMatcher.Match(pna, out bool isMultipleGmin);
                    var miejscowosc = matchResult.miejscowosc;
                    var gmina = matchResult.gmina;
                    var miastoNazwa = matchResult.miasto;
                    var gminaNazwa = matchResult.gminaNazwa;

                    if (isMultipleGmin)
                    {
                        stats.MultipleGminFound++;
                    }

                    if (miejscowosc == null)
                    {
                        // POPRAWIONE KOMUNIKATY:
                        if (gmina == null)
                        {
                            // Sytuacja 1: Nie znaleziono gminy w bazie
                            _logger.LogError($"Nie znaleziono gminy: {gminaNazwa} w powiecie {pna.Powiat}, woj. {pna.Wojewodztwo} dla kodu {pna.Kod}");
                        }
                        else if (isMultipleGmin)
                        {
                            // Sytuacja 2: Znaleziono wiele gmin o tej nazwie, ale miejscowość nie jest w żadnej
                            var gminyLista = string.Join(", ", gminyDict[$"{pna.Wojewodztwo}|{pna.Powiat}|{gminaNazwa}".ToLowerInvariant()]
                                .Select(g => g.RodzajGminy.Nazwa));
                            _logger.LogError($"Nie znaleziono miejscowości: '{miastoNazwa}' w żadnej z {gminyDict[$"{pna.Wojewodztwo}|{pna.Powiat}|{gminaNazwa}".ToLowerInvariant()].Count} gmin o nazwie '{gminaNazwa}' ({gminyLista}) dla kodu {pna.Kod}");
                        }
                        else
                        {
                            // Sytuacja 3: Znaleziono gminę, ale miejscowość nie jest w tej gminie
                            _logger.LogError($"Nie znaleziono miejscowości: '{miastoNazwa}' w gminie '{gminaNazwa}' ({gmina.RodzajGminy.Nazwa}) dla kodu {pna.Kod}");
                        }

                        stats.ErrorCount++;
                        stats.SkippedCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    // 2. Znajdź ulicę (jeśli jest)
                    var ulicaResult = ulicaMatcher.Match(pna.Ulica, miejscowosc, miastoNazwa, pna.Kod);
                    var ulica = ulicaResult.ulica;
                    var ulicaNazwa = ulicaResult.ulicaNazwa;

                    if (!string.IsNullOrEmpty(pna.Ulica) && ulica == null)
                    {
                        _logger.LogError(ulicaMatcher.GetNotFoundMessage(pna.Ulica, miejscowosc, miastoNazwa, ulicaNazwa) + $" dla kodu {pna.Kod}");
                        stats.ErrorCount++;
                    }

                    // 3. Sprawdź duplikaty
                    if (recordBuilder.IsDuplicate(pna.Kod, miejscowosc.Id, ulica?.Id))
                    {
                        stats.DuplicateCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    // 4. Utwórz rekord
                    var kodPocztowy = recordBuilder.Build(pna, miejscowosc, ulica);
                    pendingRecords.Add(kodPocztowy);

                    if (ulica != null || string.IsNullOrEmpty(pna.Ulica))
                    {
                        stats.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Błąd: {pna.Kod}: {ex.Message}");
                    stats.ErrorCount++;
                }

                stats.ProcessedCount++;

                // Zapisz partię
                if (pendingRecords.Count >= batchSize)
                {
                    await SaveBatchAsync(pendingRecords, stats);
                }

                // Flush log
                if (stats.ProcessedCount % logFlushInterval == 0)
                {
                    await _logger.FlushAsync();
                }

                // Raportuj postęp
                if (stats.ProcessedCount % reportInterval == 0 || stats.ProcessedCount == pnaData.Count)
                {
                    stats.CorrectedMiejscowosciCount = miejscowoscMatcher.CorrectedCount;
                    stats.CorrectedUliceCount = ulicaMatcher.CorrectedCount;

                    progressInfo.ProcessedCount = stats.ProcessedCount;
                    progressInfo.SuccessCount = stats.SuccessCount;
                    progressInfo.ErrorCount = stats.ErrorCount;
                    progressInfo.CurrentOperation = $"Przetworzono {stats.ProcessedCount}/{pnaData.Count} (Sukces: {stats.SuccessCount}, Błędy: {stats.ErrorCount}, Korekty: M={stats.CorrectedMiejscowosciCount}, U={stats.CorrectedUliceCount})";
                    progress?.Report(progressInfo);
                }
            }

            // Zapisz pozostałe
            if (pendingRecords.Count > 0)
            {
                await SaveBatchAsync(pendingRecords, stats);
            }

            await _logger.FlushAsync();

            // Raport końcowy
            stats.CorrectedMiejscowosciCount = miejscowoscMatcher.CorrectedCount;
            stats.CorrectedUliceCount = ulicaMatcher.CorrectedCount;

            progressInfo.ProcessedCount = stats.ProcessedCount;
            progressInfo.SuccessCount = stats.SuccessCount;
            progressInfo.ErrorCount = stats.ErrorCount;
            progressInfo.CurrentOperation = "Zakończono ładowanie kodów pocztowych";
            progress?.Report(progressInfo);

            await _logger.WriteSummaryAsync(stats.FormatSummary(pnaData.Count));
        }

        private async Task SaveBatchAsync(List<KodPocztowy> pendingRecords, LoadStatistics stats)
        {
            try
            {
                await _context.KodyPocztowe.AddRangeAsync(pendingRecords);
                await _context.SaveChangesAsync();
                pendingRecords.Clear();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError($"BŁĄD ZAPISU PARTII (batch {stats.ProcessedCount / 1000}):");
                _logger.LogError($"Message: {dbEx.Message}");
                _logger.LogError($"Inner: {dbEx.InnerException?.Message}");

                for (int i = 0; i < Math.Min(5, pendingRecords.Count); i++)
                {
                    var rec = pendingRecords[i];
                    _logger.LogError($"  Rekord {i}: Kod={rec.Kod}, MiejscowoscId={rec.MiejscowoscId}, UlicaId={rec.UlicaId}");
                }

                await _logger.FlushAsync();
                throw;
            }
        }
    }
}
