using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Helpers;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Ładuje kody pocztowe z tablicy PNA do struktury hierarchicznej.
    /// </summary>
    public class KodyPocztoweLoaderService : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly PostalCodesLogger _logger;
        private readonly PnaCorrectionHelper _pnaCorrections; // 🆕 DODANE
        string sKorekcja = "";

        public string LogFilePath => _logger.LogFilePath;

        public KodyPocztoweLoaderService(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new PostalCodesLogger(appDataPath);
            _pnaCorrections = new PnaCorrectionHelper(appDataPath ?? string.Empty); // 🆕 DODANE
            
            Console.WriteLine($"[KodyPocztoweLoaderService] Załadowano {_pnaCorrections.Count} korekt PNA");
        }

        public async Task LoadAsync(
            List<Pna> pnaData,
            IProgress<LoadProgressInfo>? progress = null)
        {
            Console.WriteLine($"[KodyPocztoweLoaderService] ========== START LoadAsync ==========");
            Console.WriteLine($"[KodyPocztoweLoaderService] PNA count: {pnaData.Count}");
            
            Console.WriteLine($"[KodyPocztoweLoaderService] Wywołuję _logger.InitializeAsync()...");
            await _logger.InitializeAsync();
            Console.WriteLine($"[KodyPocztoweLoaderService] ✓ _logger.InitializeAsync() zakończone");

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

            progressInfo.CurrentOperation = "Ładowanie danych hierarchicznych...";
            progress?.Report(progressInfo);

            // Buduj słowniki
            var dictionaryBuilder = new KodyPocztoweDictionaryBuilder(_context);
            var gminyDict = await dictionaryBuilder.BuildGminyDictionaryAsync();
            var miastaDict = await dictionaryBuilder.BuildMiastaDictionaryAsync();
            var uliceDict = await dictionaryBuilder.BuildUliceDictionaryAsync();

            // Inicjalizuj matchery - PRZEKAŻ LOGGER
            var miastoMatcher = new MiastoMatcher(gminyDict, miastaDict, _logger);
            var ulicaMatcher = new UlicaMatcher(uliceDict,_logger);

            progressInfo.CurrentOperation = "Przetwarzanie kodów pocztowych...";
            progress?.Report(progressInfo);

            var stats = new LoadStatistics();
            stats.CorrectionsCount = 0;

            var pendingRecords = new List<KodPocztowy>();
            const int reportInterval = 500;
            const int logFlushInterval = 100;

            //foreach (var pna in pnaData.Where(x=>x.Ulica=="Cicha" && x.Miasto=="Warszawa"))
            foreach (var pna_raw in pnaData)
            {
                try
                {
                    var pna_src = pna_raw;
                    // Usunięcie cudzysłowów charakterystycznych dla plików CSV
   
                    pna_src.Miasto =UliceUtils.RemoveQuote(pna_src.Miasto);
                    pna_src.Ulica = UliceUtils.RemoveQuote(pna_src.Ulica);
                    pna_src.Numery = UliceUtils.RemoveQuote(pna_src.Numery);
                    sKorekcja = "";
                    Pna pna=pna_src;
                    // 🆕 KROK 1: Zastosuj korektę jeśli istnieje
                    if (KorektaPna(pna, out var pnaCorrected)){
                        stats.CorrectionsCount++;
                        sKorekcja = "Tak";
                        
                        if (pnaCorrected.Kod != "???")
                        {
                            pna = pnaCorrected;
                        }
                    };

                    // 1a. Znajdź miasto
                    var matchResult = miastoMatcher.Match(pna, out bool isMultipleGmin);
                    var miasto = matchResult.miasto;

                    var gmina = matchResult.gmina;
                    var miastoNazwa = matchResult.miastoNazwa;
                    var gminaNazwa = matchResult.gminaNazwa;

                    if (isMultipleGmin)
                    {
                        stats.MultipleGminFound++;
                    }

                    if (miasto == null)
                    {
                         if (gmina == null)
                        {
                            // Sytuacja 1: Nie znaleziono gminy w bazie
                            _logger.LogError($"Nie znaleziono gminy: {gminaNazwa} w powiecie {pna.Powiat}, woj. {pna.Wojewodztwo} dla kodu {pna.Kod}");
                        }
                        else if (isMultipleGmin)
                        {
                            // Sytuacja 2: Znaleziono wiele gmin o tej nazwie, ale miasto nie jest w żadnej
                            var gminyLista = string.Join(", ", gminyDict[$"{pna.Wojewodztwo}|{pna.Powiat}|{gminaNazwa}".ToLowerInvariant()]
                                .Select(g => g.RodzajGminy.Nazwa));
                            _logger.LogError($"Nie znaleziono miasta: '{miastoNazwa}' w żadnej z {gminyDict[$"{pna.Wojewodztwo}|{pna.Powiat}|{gminaNazwa}".ToLowerInvariant()].Count} gmin o nazwie '{gminaNazwa}' ({gminyLista}) dla kodu {pna.Kod}");
                        }
                        else
                        {
                            // Sytuacja 3: Znaleziono gminę, ale miasto nie jest w tej gminie
                            _logger.LogError($"Nie znaleziono miasta: '{miastoNazwa}' w gminie '{gminaNazwa}' ({gmina.RodzajGminy.Nazwa}) dla kodu {pna.Kod}");
                        }

                        stats.ErrorCount++;
                        stats.SkippedCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    // 2. Znajdź ulicę (jeśli jest)
                    string? sUlica = pna.Ulica.Replace("-go","");
                    
                    // Rozkładamy ulicę na prefix i część pozostałą
                    (string sPrefix,sUlica) = UliceUtils.SplitStreetPrefix(sUlica);
                    // Usuwamy duplikat prefiksu, przykład os. Osiedle Kolorowe
                    sUlica = UliceUtils.RemoveStreetTypeDuplication(sPrefix,sUlica);
                    
                    (var ulica,var ulicaNazwa) = ulicaMatcher.Match(pna.Kod, pna.Wojewodztwo, pna.Powiat, gminaNazwa, miasto, pna.Dzielnica, sPrefix, sUlica);
                    
                    if (!string.IsNullOrEmpty(pna.Ulica) && ulica == null)
                    {
                        _logger.LogError(ulicaMatcher.GetNotFoundMessage(pna.Ulica, miasto, miastoNazwa, sKorekcja) + $" dla kodu {pna.Kod}");
                        stats.ErrorCount++;
                        stats.SkippedCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    string dzielnica = "";
                    if (miasto.Nazwa == "Warszawa" && pna.Dzielnica == "Wesoła")
                    {
                        dzielnica = pna.Dzielnica;
                    }

                    // 4. Utwórz rekord

                    var kodPocztowy = new KodPocztowy
                    {
                        Kod = pna.Kod,
                        Numery = pna.Numery,
                        MiastoId = miasto.Id,
                        UlicaId = ulica?.Id ?? -1
                    };

                    pendingRecords.Add(kodPocztowy);

                    if (ulica != null || string.IsNullOrEmpty(pna.Ulica))
                    {
                        stats.SuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Błąd: {pna_raw.Kod}: {ex.Message}");
                    stats.ErrorCount++;
                }

                stats.ProcessedCount++;
                
                // Raportuj postęp
                if (stats.ProcessedCount % reportInterval == 0 || stats.ProcessedCount == pnaData.Count)
                {
                    progressInfo.ProcessedCount = stats.ProcessedCount;
                    progressInfo.SuccessCount = stats.SuccessCount;
                    progressInfo.ErrorCount = stats.ErrorCount;
                    progressInfo.CurrentOperation = $"Przetworzono {stats.ProcessedCount}/{pnaData.Count} (Sukces: {stats.SuccessCount}, Błędy: {stats.ErrorCount}, Korekty: {stats.CorrectionsCount})";
                    progress?.Report(progressInfo);
                }
            }

            // Zapisz pozostałe
            if (pendingRecords.Count > 0)
            {
                var uniqueRecords = pendingRecords
                    .GroupBy(x => new { x.Kod, x.MiastoId, x.UlicaId, x.Numery })
                    .Select(g => g.First())
                    .ToList();
                await SaveBatchAsync(uniqueRecords, stats);
            }

            progressInfo.ProcessedCount = stats.ProcessedCount;
            progressInfo.SuccessCount = stats.SuccessCount;
            progressInfo.ErrorCount = stats.ErrorCount;
            progressInfo.CurrentOperation = "Zakończono ładowanie kodów pocztowych";
            progress?.Report(progressInfo);

           
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
                
                // ✅ DODAJ: Wyświetl pełny inner exception
                var innerEx = dbEx.InnerException;
                while (innerEx != null)
                {
                    _logger.LogError($"Inner Exception: {innerEx.Message}");
                    _logger.LogError($"Inner Type: {innerEx.GetType().Name}");
                    innerEx = innerEx.InnerException;
                }

                // ✅ Pokaż pierwsze 10 rekordów z tej partii
                for (int i = 0; i < Math.Min(10, pendingRecords.Count); i++)
                {
                    var rec = pendingRecords[i];
                    _logger.LogError($"  Rekord {i}: Kod={rec.Kod}, MiastoId={rec.MiastoId}, UlicaId={rec.UlicaId}, Numery={rec.Numery}");
                }

                throw;
            }
        }

        /// <summary>
        /// 🆕 Stosuje korektę do rekordu PNA jeśli istnieje w słowniku korekt
        /// </summary>
        /// <param name="pna">Oryginalny rekord PNA</param>
        /// <returns>Skorygowany rekord PNA lub oryginalny jeśli brak korekty</returns>
        private bool KorektaPna(Pna pna, out Pna corrected)
        {
            corrected = _pnaCorrections.TryCorrect(pna);
            
            if (corrected != null)
            {
                _logger.LogInfo($"✓ Korekta PNA: '{pna.Kod}' '{pna.Miasto}/{pna.Ulica}' -> '{corrected.Kod}' '{corrected.Miasto}/{corrected.Ulica}'");
                return true;
            }

            return false; // Bez zmian
        }

        // ✅ Dispose loggera
        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
}