using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Dictionaries.CechyUlic;
using Microsoft.EntityFrameworkCore;


namespace AddressLibrary.Services.KodyPocztoweLoader
{
    /// <summary>
    /// Ładuje kody pocztowe z tablicy PNA do struktury hierarchicznej.
    /// </summary>
    public class KodyPocztoweLoaderService : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly PostalCodesLogger _logger;
        private readonly PostalCodesLogger _fuzzyLogger;
        private readonly PostalCodesLogger _errorLogger;
        private readonly PnaCorrectionHelper _pnaCorrections;
        private readonly NameCorrectionHelper _corrections;
        private readonly PnaErrorExcelWriter _excelWriter;
        private readonly string _appDataPath;
        private string sKorekcja = "";

        public string LogFilePath => _logger.LogFilePath;
        public string FuzzyLogFilePath => _fuzzyLogger.LogFilePath;
        public string ErrorLogFilePath => _errorLogger.LogFilePath;

        public KodyPocztoweLoaderService(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath ?? string.Empty;
            _logger = new PostalCodesLogger(appDataPath);
            _fuzzyLogger = new PostalCodesLogger(appDataPath, "PostalCodesLoader_Fuzzy.txt");
            _errorLogger = new PostalCodesLogger(appDataPath, "PostalCodesLoader_Error.txt");
            _pnaCorrections = new PnaCorrectionHelper(_appDataPath);
            _corrections = new NameCorrectionHelper(appDataPath);
            _excelWriter = new PnaErrorExcelWriter();

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
            await _fuzzyLogger.InitializeAsync();
            await _errorLogger.InitializeAsync(); // ✅ NOWE
            Console.WriteLine($"[KodyPocztoweLoaderService] ✓ _logger.InitializeAsync() zakończone");

            // Zainicjalizuj CechyUlicUtils przez dedykowany cache
            _logger.LogInfo("=== Ładowanie słownika StreetPrefixes z bazy CechyUlic ===");
            var cechyCache = new AddressLibrary.Cache.CechyUlicCache(_context);
            await cechyCache.InitializeAsync();
            _logger.LogInfo($"✓ Załadowano {CechyUlicUtils.StreetPrefixes.Count} cech ulic do StreetPrefixes");

            // DODANO: Wyczyść tabelę KodyPocztowe przed rozpoczęciem ładowania
            var progressInfo = new LoadProgressInfo
            {
                TotalCount = pnaData.Count,
                CurrentOperation = "Czyszczenie tabeli KodyPocztowe..."
            };
            progress?.Report(progressInfo);

            // ✅ ZMIENIONO: Loguj do error loggera zamiast głównego
            _logger.LogInfo("=== Rozpoczęcie czyszczenia tabeli KodyPocztowe ===");

            try
            {
                // Usuń wszystkie rekordy z tabeli KodyPocztowe
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM KodyPocztowe");
                _logger.LogInfo("✓ Tabela KodyPocztowe została wyczyszczona");
            }
            catch (Exception ex)
            {
                // ✅ ZMIENIONO: Loguj błędy do error loggera
                _errorLogger.LogError($"✗ Błąd podczas czyszczenia tabeli: {ex.Message}");
                throw;
            }

            progressInfo.CurrentOperation = "Ładowanie danych hierarchicznych...";
            progress?.Report(progressInfo);

            // Buduj słowniki
            var dictionaryBuilder = new KodyPocztoweDictionaryBuilder(_context);
            var gminyDict = await dictionaryBuilder.BuildGminyDictionaryAsync();
            var miastaDict = await dictionaryBuilder.BuildMiastaDictionaryAsync();
            var uliceDict = await dictionaryBuilder.BuildUliceDictionaryAsync();

            // ✅ POPRAWKA: Utwórz i zainicjalizuj StreetParser
            _logger.LogInfo("=== Inicjalizacja StreetParser ===");

            var streetParser = new AddressLibrary.Services.AddressSearch.StreetParser(_context);

            try
            {
                await streetParser.InitializeAsync();
                _logger.LogInfo($"✓ StreetParser zainicjalizowany");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Błąd inicjalizacji StreetParser: {ex.Message}");
                throw;
            }

            // Przekaż error logger do matcherów
            var miastoMatcher = new MiastoMatcher(gminyDict, miastaDict, _logger, _fuzzyLogger, _errorLogger);
            var ulicaMatcher = new UlicaMatcher(uliceDict, _logger, _fuzzyLogger, _errorLogger, streetParser);

            _logger.LogInfo($"✓ UlicaMatcher utworzony z StreetParser");

            progressInfo.CurrentOperation = "Przetwarzanie kodów pocztowych...";
            progress?.Report(progressInfo);

            var stats = new LoadStatistics();
            stats.CorrectionsCount = 0;

            var pendingRecords = new List<KodPocztowy>();
            const int reportInterval = 500;

//             foreach (var pna_raw in pnaData.Where(x => x.Miasto=="Kraków" && x.Ulica.Contains("Halszki")))
            foreach (var pna_raw in pnaData)
            {
                try
                {
                    // Skopiuj dane PNA przez wartość, żeby modyfikacje nie wpływały na źródłowy obiekt
                    var pna_src = CloneHelper.Klonuj(pna_raw) ?? new Pna();

                    pna_src.Miasto = UliceUtils.RemoveQuote(pna_src.Miasto);
                    pna_src.Ulica = UliceUtils.RemoveQuote(pna_src.Ulica);
                    pna_src.Numery = UliceUtils.RemoveQuote(pna_src.Numery);
                    sKorekcja = string.Empty;
                    var pna = CloneHelper.Klonuj(pna_src) ?? new Pna();

                    if (KorektaPna(pna, out var pnaCorrected))
                    {
                        stats.CorrectionsCount++;
                        sKorekcja = "Tak";

                        if (pnaCorrected.Kod != "???")
                        {
                            pna = pnaCorrected;
                        }
                        else
                        {
//                            _logger.LogInfo($"{FormatPnaRecord(pna)}|Błąd w PNA, pozycja zignorowana");
                            continue;
                        }
                    }

                    if (_corrections.TryCorrect("U", pna.Ulica, out var correctedStreet))
                    {
                        Console.WriteLine($"Skorygowano ulicę: '{pna.Ulica}' -> '{correctedStreet}'");
                        pna.Ulica = correctedStreet;
                    }

                    // 1a. Znajdź miasto

                    var matchResult = miastoMatcher.Match(pna);


                    if (matchResult == null)
                    {
                        _errorLogger.LogError($"Nie znaleziono miasta '{pna.Miasto}' w gminie '{pna.Gmina}/{pna.Powiat}/{pna.Wojewodztwo}' dla kodu {pna.Kod}");
                        continue;
                    }


                    if (matchResult.Count>1)
                    {
                        // Wstawianie kodów pocztowych dla wszystkich wsi, osad i dzielnic o kodzie "Abisynia" czy "Wyźrzał"
                        foreach (var elem in matchResult)
                        {
                            var kodPocztowy2 = new KodPocztowy
                            {
                                Kod = pna.Kod,
                                Numery = pna.Numery,
                                MiastoId = elem.Id,
                                UlicaId = -1
                            };

//                            _errorLogger.LogInfo($"Wstawianie '{pna.Miasto}/{elem.RodzajMiasta.Nazwa}' w gminie '{pna.Gmina}/{pna.Powiat}/{pna.Wojewodztwo}' dla kodu {pna.Kod}");
                            pendingRecords.Add(kodPocztowy2);
                        }
                        continue;
                    }

                    var miasto = matchResult[0];
                    var sCecha = "";
                    var sUlica = pna.Ulica;
                    var pattern = "inne";
                    // w korekcie umieściłem napis "inne" który oznacza, że nie mam dodawać "ul." do ulicy PNA
                    if (pna.Ulica.StartsWith(pattern + " "))
                    {
                        sUlica = pna.Ulica.Substring(pattern.Length + 1);
                    } else 
                    {
                        // Jeśli skorygowana ulica nie zaczyna się od inne, to staramy się nadać cechę "ul."
                        (sCecha, sUlica) = CechyUlicUtils.SplitStreetPrefix(pna.Ulica);
                        (string sCecha2, string sUlica2) = CechyUlicUtils.SplitStreetPrefix(sUlica);
                        if (sCecha2 != "")
                        {
                            // przypadek 'ul. Plac' ma dać 'pl.'
                            sCecha = sCecha2;
                            sUlica = sUlica2;
                        }

                        // Sprawdzamy czy nazwa ulicy sama z siebie nie jest Cechą czyli Rynek, Zaułek itd.
                        CechyUlicUtils.StreetPrefixes.TryGetValue(sUlica, out var wynik);

                        if (wynik == null && sCecha == "") sCecha = "ul.";
                    }

//
//  I główne wywołanie - szukamy ulicy w znalezionym mieście
//
                    var listaUlic = ulicaMatcher.Match(pna.Kod, pna.Wojewodztwo, pna.Powiat, miasto.Gmina.Nazwa, miasto, pna.Dzielnica, sCecha, sUlica,out string ulicaNazwa, out string info);

                    if (!string.IsNullOrEmpty(pna.Ulica) && listaUlic == null)
                    {
                        var ulicaMsg = ulicaMatcher.GetNotFoundMessage(pna.Ulica, miasto, miasto.Nazwa, sKorekcja,info);
                        _errorLogger.LogError($"{FormatPnaRecord(pna)}|{ulicaMsg}");
                        //                        _excelWriter.Add(pna, $"Brak ulicy: {ulicaMsg}");
                        _excelWriter.Add(pna_src, "Brak ulicy");
                        stats.ErrorCount++;
                        stats.SkippedCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(pna.Ulica) && listaUlic.Count()>1)
                    {
                        var ulicaMsg = $"Zbyt wiele ulic (?info)";
                        _errorLogger.LogError($"{FormatPnaRecord(pna)}|{ulicaMsg}");
                        _excelWriter.Add(pna_src, "Zbyt wiele ulic");
                        stats.ErrorCount++;
                        stats.SkippedCount++;
                        stats.ProcessedCount++;
                        continue;
                    }

                    var ulica = listaUlic != null ? listaUlic[0] : null;
                    string dzielnica = "";
                    if (miasto.Nazwa == "Warszawa" && pna.Dzielnica == "Wesoła")
                    {
                        dzielnica = pna.Dzielnica;
                    }

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
                    // ✅ ZMIENIONO: Loguj do error loggera
                    _errorLogger.LogError($"Błąd: {pna_raw.Kod}: {ex.Message}");
                    stats.ErrorCount++;
                }

                stats.ProcessedCount++;

                if (stats.ProcessedCount % reportInterval == 0 || stats.ProcessedCount == pnaData.Count)
                {
                    progressInfo.ProcessedCount = stats.ProcessedCount;
                    progressInfo.SuccessCount = stats.SuccessCount;
                    progressInfo.ErrorCount = stats.ErrorCount;
                    progressInfo.CurrentOperation = $"Przetworzono {stats.ProcessedCount}/{pnaData.Count} (Sukces: {stats.SuccessCount}, Błędy: {stats.ErrorCount}, Korekty: {stats.CorrectionsCount})";
                    progress?.Report(progressInfo);
                }
            }

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

            // Zapisz błędy do Excela
            if (_excelWriter.Count > 0)
            {
                _excelWriter.Save(_appDataPath);
                _logger.LogInfo($"Zapisano {_excelWriter.Count} błędów do BledyPnaPropozycje.xlsx");
            }
        }

        private async Task SaveBatchAsync(List<KodPocztowy> pendingRecords, LoadStatistics stats)
        {
            try
            {
                await _context.KodyPocztowe.AddRangeAsync(pendingRecords);
                await _context.SaveChangesAsync();
                pendingRecords.Clear();
            }
            catch (Exception ex)
            {
                // ✅ AWARYJNE ZAPISANIE BŁĘDU DO PLIKU
                var errorFile = Path.Combine(Path.GetTempPath(), $"KodyPocztoweError_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"ERROR TIME: {DateTime.Now}");
                sb.AppendLine($"ERROR TYPE: {ex.GetType().FullName}");
                sb.AppendLine($"ERROR MESSAGE: {ex.Message}");
                sb.AppendLine($"\nSTACK TRACE:\n{ex.StackTrace}");

                var innerEx = ex.InnerException;
                int level = 1;
                while (innerEx != null)
                {
                    sb.AppendLine($"\n{'=' * 60}");
                    sb.AppendLine($"INNER EXCEPTION LEVEL {level}");
                    sb.AppendLine($"{'=' * 60}");
                    sb.AppendLine($"Type: {innerEx.GetType().FullName}");
                    sb.AppendLine($"Message: {innerEx.Message}");
                    sb.AppendLine($"Stack Trace:\n{innerEx.StackTrace}");

                    if (innerEx is Microsoft.Data.SqlClient.SqlException sqlEx)
                    {
                        sb.AppendLine($"\nSQL ERROR DETAILS:");
                        sb.AppendLine($"  Number: {sqlEx.Number}");
                        sb.AppendLine($"  State: {sqlEx.State}");
                        sb.AppendLine($"  Server: {sqlEx.Server}");
                        sb.AppendLine($"  Procedure: {sqlEx.Procedure}");
                        sb.AppendLine($"  Line Number: {sqlEx.LineNumber}");

                        foreach (Microsoft.Data.SqlClient.SqlError err in sqlEx.Errors)
                        {
                            sb.AppendLine($"\n  SQL Error:");
                            sb.AppendLine($"    Message: {err.Message}");
                            sb.AppendLine($"    Number: {err.Number}");
                            sb.AppendLine($"    State: {err.State}");
                            sb.AppendLine($"    Class: {err.Class}");
                        }
                    }

                    innerEx = innerEx.InnerException;
                    level++;
                }

                sb.AppendLine($"\n{'=' * 60}");
                sb.AppendLine($"SAMPLE RECORDS ({pendingRecords.Count} total):");
                sb.AppendLine($"{'=' * 60}");
                for (int i = 0; i < Math.Min(10, pendingRecords.Count); i++)
                {
                    var rec = pendingRecords[i];
                    sb.AppendLine($"[{i}] Kod={rec.Kod}, MiastoId={rec.MiastoId}, UlicaId={rec.UlicaId}, Numery={rec.Numery?.Substring(0, Math.Min(100, rec.Numery.Length))}");
                }

                File.WriteAllText(errorFile, sb.ToString());

                Console.WriteLine($"\n\n❌❌❌ CRITICAL ERROR ❌❌❌");
                Console.WriteLine($"Error details saved to: {errorFile}");
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                Console.WriteLine($"\n");

                throw;
            }
        }

        private bool KorektaPna(Pna pna, out Pna corrected)
        {
            corrected = _pnaCorrections.TryCorrect(pna);

            if (corrected != null)
            {
//                _logger.LogInfo($"✓ Korekta PNA: '{pna.Kod}' '{pna.Miasto}/{pna.Ulica}' -> '{corrected.Kod}' '{corrected.Miasto}/{corrected.Ulica}'");
                return true;
            }

            return false;
        }

        /// <summary>
        /// ✅ NOWA METODA: Formatuje rekord PNA do logu (wszystkie pola oddzielone pipe |)
        /// </summary>
        private string FormatPnaRecord(Pna pna)
        {
            return $"{pna.Kod}|{pna.Miasto}|{pna.Dzielnica}|{pna.Ulica}|{pna.Numery}|{pna.Wojewodztwo}|{pna.Powiat}|{pna.Gmina}";
        }

        public void Dispose()
        {
            _logger?.Dispose();
            _fuzzyLogger?.Dispose();
            _errorLogger?.Dispose();
        }
    }
}