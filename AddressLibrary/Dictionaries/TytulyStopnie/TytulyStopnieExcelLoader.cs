using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Services;

namespace AddressLibrary.Dictionaries.TytulyStopnie
{
    /// <summary>
    /// Serwis do ładowania słownika TytulyStopnie z pliku Excel do bazy danych
    /// </summary>
    public class TytulyStopnieExcelLoader
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly GeneralLogger _logger;

        public TytulyStopnieExcelLoader(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new GeneralLogger(appDataPath, "LoadTytulyStopnie.txt", "Log TytulyStopnie");
        }

        /// <summary>
        /// Ładuje dane z pliku Excel TytulyStopnie.xlsx do tabeli TytulyStopnie
        /// Struktura kolumn:
        /// A = Nazwa (pełna nazwa, np. "generał")
        /// B = Dopelniacz (forma dopełniacza, np. "generała")
        /// C = Skrot (skrót, np. "gen.")
        /// </summary>
        public async Task<LoadResult> LoadFromExcelAsync(IProgress<LoadProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new LoadResult();
            var excelPath = Directories.GetExcelFilePath("TytulyStopnie.xlsx");

            _logger.LogInfo("=== Rozpoczęcie ładowania TytulyStopnie ===");

            try
            {
                await EnsureDefaultRecordExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd podczas dodawania domyślnego rekordu: {ex.Message}");
            }

            try
            {
                // Usuń wszystkie rekordy oprócz Id = -1
                _logger.LogInfo("Usuwanie istniejących rekordów (oprócz Id = -1)...");
                var deletedCount = await _context.TytulyStopnie
                    .Where(t => t.Id != -1)
                    .ExecuteDeleteAsync();
                _logger.LogInfo($"Usunięto {deletedCount} rekordów");

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Usunięto {deletedCount} starych rekordów"
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
                var tytulyFromExcel = new List<TytulStopien>();

                foreach (var row in rows)
                {
                    var nazwa      = row["Mianownik"]?.Trim();
                    var dopelniacz = row["Dopełniacz"]?.Trim();
                    var skrot      = row["Skrót"]?.Trim();

                    _logger.LogInfo($"Wiersz {row.RowNumber}: Nazwa='{nazwa}', Dopelniacz='{dopelniacz}', Skrot='{skrot}'");

                    if (!string.IsNullOrWhiteSpace(nazwa) && !string.IsNullOrWhiteSpace(skrot) && !string.IsNullOrWhiteSpace(dopelniacz))
                        tytulyFromExcel.Add(new TytulStopien { Nazwa = nazwa, Skrot = skrot, Dopelniacz = dopelniacz });
                    else
                        _logger.LogWarning($"Wiersz {row.RowNumber}: Pominięto - brak wymaganych danych");
                }

                result.TotalCount = tytulyFromExcel.Count;
                _logger.LogInfo($"Wczytano {result.TotalCount} wpisów z Excel");

                // Wyświetl wszystkie wczytane rekordy
                _logger.LogInfo("=== Lista wczytanych tytułów ===");
                foreach (var t in tytulyFromExcel)
                {
                    _logger.LogInfo($"  Nazwa='{t.Nazwa}', Dopelniacz='{t.Dopelniacz}', Skrot='{t.Skrot}'");
                }

                progress?.Report(new LoadProgress
                {
                    CurrentOperation = $"Dodawanie do bazy danych ({result.TotalCount} wpisów)...",
                    TotalCount = result.TotalCount
                });

                // Dodaj nowe rekordy do bazy
                await _context.TytulyStopnie.AddRangeAsync(tytulyFromExcel);
                await _context.SaveChangesAsync();

                result.InsertedCount = tytulyFromExcel.Count;
                result.ProcessedCount = tytulyFromExcel.Count;

                _logger.LogInfo($"Zakończono: Dodano: {result.InsertedCount} nowych rekordów");

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
            await DefaultRecordHelper.EnsureTytulStopienDefaultAsync(_context, _logger);
        }
    }
}