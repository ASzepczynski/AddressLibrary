using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Services;

namespace AddressLibrary.Dictionaries.CechyUlic
{
    /// <summary>
    /// Serwis do ładowania słownika CechyUlic z pliku Excel do bazy danych
    /// </summary>
    public class CechyUlicExcelLoader
    {
        private readonly AddressDbContext _context;
        private readonly GeneralLogger _logger;

        public CechyUlicExcelLoader(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _logger = new GeneralLogger(appDataPath, "LoadCechyUlic.txt", "Log CechyUlic");
        }

        /// <summary>
        /// Ładuje dane z pliku Excel CechyUlic.xlsx do tabeli CechyUlic
        /// Plik Excel znajduje się w AddressLibrary/AppData/Dictionaries/
        /// Struktura kolumn:
        /// A = Nazwa (pełna nazwa, np. "ulica")
        /// B = Skrot (skrót, np. "ul.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            
            // ✅ POPRAWKA: Szukaj pliku w AddressLibrary/AppData/Dictionaries/
            var excelPath = Directories.GetExcelFilePath("CechyUlic.xlsx");

            _logger.LogInfo("=== Rozpoczęcie ładowania CechyUlic ===");
            _logger.LogInfo($"Ścieżka do pliku Excel: {excelPath}");

            try
            {
                // Upewnij się, że rekord z ID = -1 istnieje
                await EnsureDefaultRecordExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas dodawania domyślnego rekordu: {ex.Message}");
            }

            try
            {
                // KROK 1: Usuń KodyPocztowe (najwyższy poziom w hierarchii FK)
                _logger.LogInfo("Usuwanie rekordów z tabeli KodyPocztowe (oprócz Id = -1)...");
                var deletedKodyPocztowe = await _context.KodyPocztowe
                    .Where(k => k.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usunięto {deletedKodyPocztowe} rekordów z KodyPocztowe");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usunięto {deletedKodyPocztowe} kodów pocztowych"
                });

                // KROK 2: Usuń Ulice (mają FK do CechyUlic)
                _logger.LogInfo("Usuwanie rekordów z tabeli Ulice (oprócz Id = -1)...");
                var deletedUlice = await _context.Ulice
                    .Where(u => u.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usunięto {deletedUlice} rekordów z Ulice");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usunięto {deletedUlice} ulic"
                });

                // KROK 3: Teraz można bezpiecznie usunąć CechyUlic
                _logger.LogInfo("Usuwanie istniejących rekordów z CechyUlic (oprócz Id = -1)...");
                var deletedCechy = await _context.CechyUlic
                    .Where(c => c.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usunięto {deletedCechy} rekordów z CechyUlic");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usunięto {deletedCechy} starych rekordów CechyUlic"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas usuwania rekordów: {ex.Message}");
                result.ErrorMessage = $"Błąd podczas usuwania rekordów: {ex.Message}";
                return result;
            }

            if (!File.Exists(excelPath))
            {
                _logger.LogError($"Plik nie istnieje: {excelPath}");
                result.ErrorMessage = $"Plik nie istnieje: {excelPath}";
                return result;
            }

            try
            {
                progress?.Report(new LoadProgress { CurrentOperation = "Odczyt pliku Excel..." });

                var rows = ExcelTableReader.Read(excelPath);
                var cechyFromExcel = new List<CechaUlicy>();

                foreach (var row in rows)
                {
                    var nazwa = row["Nazwa"]?.Trim();
                    var skrot = row["Skrot"]?.Trim();

              //      _logger.LogInfo($"Wiersz {row.RowNumber}: Nazwa='{nazwa}', Skrot='{skrot}'");

                    if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot))
                        cechyFromExcel.Add(new CechaUlicy { Nazwa = nazwa, Skrot = skrot });
                    else
                        _logger.LogWarning($"Wiersz {row.RowNumber}: Pominięto - brak wymaganych danych");
                }

                result.TotalCount = cechyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

                // Wyświetl wszystkie wczytane rekordy
                //_logger.LogInfo("=== Lista wczytanych cech ulic ===");
                //foreach (var c in cechyFromExcel)
                //{
                //    _logger.LogInfo($"  Nazwa='{c.Nazwa}', Skrot='{c.Skrot}'");
                //}

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Dodawanie do bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Dodaj nowe rekordy do bazy
                await _context.CechyUlic.AddRangeAsync(cechyFromExcel);
                await _context.SaveChangesAsync();

                result.InsertedCount = cechyFromExcel.Count;
                result.ProcessedCount = cechyFromExcel.Count;

                _logger.LogInfo($"Zakończono: Dodano: {result.InsertedCount} nowych rekordów");
                _logger.LogInfo("=== Zakończenie ładowania CechyUlic ===");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = "Zakończono",
                    TotalCount = result.TotalCount,
                    ProcessedCount = result.ProcessedCount,
                    IsCompleted = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }


        private async Task EnsureDefaultRecordExistsAsync()
        {
            await DefaultRecordHelper.EnsureCechaUlicyDefaultAsync(_context, _logger);
        }
    }
}