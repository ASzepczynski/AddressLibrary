using AddressLibrary.Data;
using AddressLibrary.Models;
using AddressLibrary.Logging;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;
using AddressLibrary.Helpers;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do walidacji nazw ulic z TerytUlic względem słownika TerytUlicPoprawki.xlsx
    /// </summary>
    public class TerytUlicPoprawkiValidatorService
    {
        private readonly AddressDbContext _context;
        private readonly string _appDataPath;
        private readonly PostalCodesLogger _logger;

        public TerytUlicPoprawkiValidatorService(AddressDbContext context, string appDataPath)
        {
            _context = context;
            _appDataPath = appDataPath;
            _logger = new PostalCodesLogger(appDataPath, "TerytUlic.txt");
        }

        /// <summary>
        /// Waliduje wszystkie wpisy z TerytUlic względem słownika
        /// </summary>
        public async Task<ValidatorResult> ValidateAsync(IProgress<ValidatorProgress>? progress = null)
        {
            await _logger.InitializeAsync();

            var result = new ValidatorResult();

            _logger.LogInfo("=== Rozpoczęcie walidacji TerytUlic ===");

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Ładowanie słownika TerytUlicPoprawki..."
            });

            // KROK 1: Wczytaj słownik
            var dictionary = TerytUlicPoprawkiDictionary.Load(_appDataPath,_logger);

            if (dictionary.Count == 0)
            {
                _logger.LogError("Słownik jest pusty - przerywam walidację");
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
                CurrentOperation = $"Walidacja {result.TotalCount} wpisów...",
                TotalCount = result.TotalCount
            });

            // KROK 3: Waliduj każdy wpis
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
                if (dictionary.TryGetValue(original, out var TerytUlicPoprawka))
                {
                    result.FoundCount++;

                    // ✅ Porównaj terytUlica ze znalezioną pozycją w słowniku
                    CompareAndLog(terytUlica, TerytUlicPoprawka, original);
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

            // KROK 4: Podsumowanie
            _logger.LogInfo("=== Podsumowanie walidacji ===");
            _logger.LogInfo($"Przetworzono: {result.ProcessedCount}");
            _logger.LogInfo($"Znaleziono w słowniku: {result.FoundCount}");
            _logger.LogInfo($"Brak w słowniku: {result.NotFoundCount}");
            _logger.LogInfo($"Procent pokrycia: {(result.FoundCount * 100.0 / result.TotalCount):F2}%");

            progress?.Report(new ValidatorProgress
            {
                CurrentOperation = "Zakończono walidację",
                TotalCount = result.TotalCount,
                ProcessedCount = result.ProcessedCount,
                IsCompleted = true
            });

            return result;
        }

        /// <summary>
        /// Porównuje wpis z TerytUlic z pozycją ze słownika i loguje różnice
        /// </summary>
        private void CompareAndLog(TerytUlic terytUlica, TerytUlicPoprawka TerytUlicPoprawka, string original)
        {

            (var prefiks, var reszta) = UliceUtils.RozdzielPrefiksTeryt(original);
            var poprawiona = $"{prefiks} {reszta}".Trim();

            var osoba = TerytUlicPoprawka.Tytul;
            osoba += " " + TerytUlicPoprawka.Imie;
            osoba += " " + TerytUlicPoprawka.Imie2;
            osoba += " " + TerytUlicPoprawka.Nazwisko;
            osoba += (TerytUlicPoprawka.Nazwisko2 != "" ? $"-{TerytUlicPoprawka.Nazwisko2}" : "");
            osoba += TerytUlicPoprawka.Pseudonim;

            string ulica = $"{prefiks} {TerytUlicPoprawka.Prefiks} {osoba} {TerytUlicPoprawka.Postfiks}";
            // ✅ Zastąp wielokrotne spacje jedną spacją
            ulica = Regex.Replace(ulica.Trim(), @"\s+", " ");

            ulica = ulica.Replace("\"", "");
            poprawiona = poprawiona.Replace("\"", "");

            poprawiona = Znormalizuj(poprawiona);
            if (!string.Equals(ulica, poprawiona, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Różnica|{TerytUlicPoprawka.Id}|{ulica}|{poprawiona}");
            }
        }


        private static readonly (string pelna, string skrot)[] ParyZastapien =
    {
            ("generała", "gen."),
            ("prymasa", "prym."),
            ("księdza", "ks."),
            ("świętego", "św."),
            ("braci", "br."),
            ("imienia", "im."),
            ("Curie-Skłodowskiej", "Skłodowskiej-Curie")
        };

        private static string Znormalizuj(string ulica)
        {
            var znormalizowana = ulica;

            // Zastąp pełne nazwy skrótami
            foreach (var (pelna, skrot) in ParyZastapien)
            {
                // Zamień zarówno na początku jak i w środku nazwy (case-insensitive)
                znormalizowana = Regex.Replace(
                    znormalizowana,
                    $@"\b{pelna}\b",
                    skrot,
                    RegexOptions.IgnoreCase
                );
            }

            // Usuń wielokrotne spacje
            znormalizowana = Regex.Replace(znormalizowana, @"\s+", " ").Trim();

            return znormalizowana;
        }


        /// <summary>
        /// Pobiera wartości wszystkich komórek z wiersza jako słownik (klucz = nazwa kolumny A-I)
        /// OPTYMALIZACJA: Iteruje po komórkach tylko RAZ zamiast 9 razy
        /// </summary>
        private static Dictionary<string, string> GetRowCellsDictionary(Row row, string[] sharedStrings)
        {
            var result = new Dictionary<string, string>();

            foreach (var cell in row.Elements<Cell>())
            {
                var columnName = GetColumnName(cell.CellReference?.Value);
                if (!string.IsNullOrEmpty(columnName))
                {
                    var value = GetCellValue(cell, sharedStrings);
                    if (value != null)
                    {
                        result[columnName] = value;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Wyodrębnia nazwę kolumny z referencji komórki (np. "A1" → "A", "B5" → "B")
        /// </summary>
        private static string GetColumnName(string? cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
                return string.Empty;

            // Wyodrębnij literę(y) z referencji (np. "A1" → "A", "AB123" → "AB")
            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        /// <summary>
        /// Pobiera wartość z komórki Excel (zoptymalizowana wersja z tablicą)
        /// </summary>
        private static string? GetCellValue(Cell cell, string[] sharedStrings)
        {
            if (cell.CellValue == null)
                return null;

            string value = cell.CellValue.InnerText;

            // ✅ Bezpośredni dostęp do tablicy zamiast ElementAt()
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out int index) && index >= 0 && index < sharedStrings.Length)
                {
                    return sharedStrings[index];
                }
            }

            return value;
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }

    

    /// <summary>
    /// Wynik walidacji
    /// </summary>
    public class ValidatorResult
    {
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int FoundCount { get; set; }
        public int NotFoundCount { get; set; }
    }

    /// <summary>
    /// Postęp walidacji
    /// </summary>
    public class ValidatorProgress
    {
        public string CurrentOperation { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public bool IsCompleted { get; set; }
    }
}