using AddressLibrary.Data;
using AddressLibrary.Models;
using AddressLibrary.Services.Dictionaries;
using AddressLibrary.Services.Dictionaries.TytulyStopnie;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using AddressLibrary.Helpers;
using AddressLibrary.Utils;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do ładowania i parsowania nazw ulic z TerytUlic do TerytUlicPoprawki
    /// </summary>
    public class TerytUlicPoprawkiLoaderService
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;
        private readonly TytulyStopnieDictionary _tytulyDict;
        
        private HashSet<string>? _imionaSet;
        private HashSet<string> ImionaSet
        {
            get
            {
                if (_imionaSet == null)
                {
                    _imionaSet = LoadImiona();
                }
                return _imionaSet;
            }
        }

        public TerytUlicPoprawkiLoaderService(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
            _tytulyDict = new TytulyStopnieDictionary(context);
        }

        /// <summary>
        /// Ładuje słownik imion z pliku AppData/Dictionaries/Imiona.txt
        /// </summary>
        private HashSet<string> LoadImiona()
        {
            var imiona = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(_appDataPath))
            {
                Console.WriteLine("⚠️ Brak ścieżki AppData - nie załadowano słownika imion");
                return imiona;
            }

            var imionaPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", "Imiona.txt");

            if (!File.Exists(imionaPath))
            {
                Console.WriteLine($"⚠️ Plik {imionaPath} nie istnieje");
                return imiona;
            }

            try
            {
                var lines = File.ReadAllLines(imionaPath);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    if (!string.IsNullOrWhiteSpace(trimmedLine) && !trimmedLine.StartsWith("#"))
                    {
                        var normalized = UliceUtils.RemoveDiacritics(trimmedLine.ToLowerInvariant());
                        imiona.Add(normalized);
                        imiona.Add(trimmedLine);
                    }
                }

                Console.WriteLine($"✓ Załadowano {imiona.Count} imion z {imionaPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Błąd ładowania słownika imion: {ex.Message}");
            }

            return imiona;
        }

        /// <summary>
        /// Przetwarza unikalne kombinacje Nazwa1/Nazwa2 z TerytUlic i zapisuje do TerytUlicPoprawki
        /// </summary>
        public async Task<LoadTerytUlicPoprawkiResult> LoadAsync(IProgress<LoadTerytUlicPoprawkiProgress>? progress = null)
        {
            var result = new LoadTerytUlicPoprawkiResult();

            // ✅ Załaduj słownik tytułów do pamięci
            await _tytulyDict.GetSkrotToIdMappingAsync();
            
            // ✅ Inicjalizuj TitleManager
            if (!TitleManager.IsInitialized)
            {
                var tytuly = await _tytulyDict.GetAllAsync();
                TitleManager.Initialize(tytuly);
            }

            // KROK 1: Wyczyść tabelę TerytUlicPoprawki
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM TerytUlicPoprawki");
            result.DeletedCount = await _context.Database.ExecuteSqlRawAsync("SELECT @@ROWCOUNT");

            progress?.Report(new LoadTerytUlicPoprawkiProgress
            {
                CurrentOperation = "Pobieranie unikalnych nazw ulic...",
                ProcessedCount = 0
            });

            // KROK 2: Pobierz unikalne kombinacje Nazwa1/Nazwa2/Cecha
            var uniqueStreets = await _context.TerytUlic
                .Where(u => !string.IsNullOrEmpty(u.Nazwa1))
                .Select(u => new { u.Cecha, u.Nazwa1, u.Nazwa2 })
                .Distinct()
                .ToListAsync();

            result.TotalCount = uniqueStreets.Count;

            progress?.Report(new LoadTerytUlicPoprawkiProgress
            {
                CurrentOperation = $"Przetwarzanie {result.TotalCount} unikalnych nazw...",
                TotalCount = result.TotalCount,
                ProcessedCount = 0
            });

            // KROK 3: Przetwórz każdą unikalną nazwę
            var batch = new List<TerytUlicPoprawka>();
            const int batchSize = 1000;

            for (int i = 0; i < uniqueStreets.Count; i++)
            {
                var street = uniqueStreets[i];

                var terytUlicPoprawka = ParseStreetName(street.Cecha, street.Nazwa1, street.Nazwa2);
                batch.Add(terytUlicPoprawka);

                // Zapisz batch co 1000 rekordów
                if (batch.Count >= batchSize || i == uniqueStreets.Count - 1)
                {
                    await _context.TerytUlicPoprawki.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();

                    result.InsertedCount += batch.Count;
                    batch.Clear();

                    progress?.Report(new LoadTerytUlicPoprawkiProgress
                    {
                        CurrentOperation = $"Przetworzono {result.InsertedCount}/{result.TotalCount}...",
                        TotalCount = result.TotalCount,
                        ProcessedCount = result.InsertedCount
                    });
                }
            }

            progress?.Report(new LoadTerytUlicPoprawkiProgress
            {
                CurrentOperation = "Zakończono ładowanie",
                TotalCount = result.TotalCount,
                ProcessedCount = result.InsertedCount,
                IsCompleted = true
            });

            return result;
        }

        /// <summary>
        /// Parsuje nazwę ulicy na komponenty
        /// </summary>
        private TerytUlicPoprawka ParseStreetName(string? cecha, string nazwa1, string? nazwa2)
        {
            var typ = new TerytUlicPoprawka();
            typ.Prefiks = "";
            string tytulStr = "";
            
            typ.Imie = nazwa2;
            typ.Imie2 = "";
            typ.Nazwisko = nazwa1;
            typ.Nazwisko2 = "";
            typ.Postfiks = "";

            // Zbuduj oryginalną nazwę
            var originalParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(cecha))
                originalParts.Add(cecha.Trim());
            if (!string.IsNullOrWhiteSpace(nazwa2))
                originalParts.Add(nazwa2.Trim());
            if (!string.IsNullOrWhiteSpace(nazwa1))
                originalParts.Add(nazwa1.Trim());
            typ.TerytId = string.Join(" ", originalParts);

            var nazwiskoDoSprawdzenia = TextNormalizer.MakeCorrections(nazwa1);
            var imieDoSprawdzenia = TextNormalizer.MakeCorrections(nazwa2);

            typ.Nazwisko = nazwiskoDoSprawdzenia;
            typ.Imie = imieDoSprawdzenia;

            // KROK 1: Obsługa dzielnic Zielonej Góry
            if (typ.Nazwisko.Contains("-"))
            {
                foreach (var dzielnica in UliceUtils.dzielnice_zg)
                {
                    var prefix = dzielnica + "-";
                    if (typ.Nazwisko.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        typ.Nazwisko = typ.Nazwisko.Substring(prefix.Length);
                        break;
                    }
                }
            }

            // KROK 2: Jeśli brak imienia, a nazwisko to jedno słowo, to to ulica nieosobowa
            if (string.IsNullOrEmpty(typ.Imie) && typ.Nazwisko.IndexOf(" ") < 0)
            {
                if (!typ.Nazwisko.EndsWith("ego"))
                {
                    typ.Postfiks = typ.Nazwisko;
                    typ.Nazwisko = "";
                }
                typ.Tytul = "";
                return typ;
            }

            // KROK 3: Wyekstrahowanie "Aleja", "ul." itp.
            (var RodzajUlicy, typ.Nazwisko) = UliceUtils.SplitStreetPrefix(typ.Nazwisko);
            (var RodzajUlicy2, typ.Imie) = UliceUtils.SplitStreetPrefix(typ.Imie);

            // KROK 4: Wzorce "im.", "imienia" - przeniesienie do Prefiks
            var patterns = new[] { "im.", "imienia" };

            if (!string.IsNullOrEmpty(typ.Nazwisko))
            {
                foreach (var pattern in patterns)
                {
                    var prefixWithSpace = pattern + " ";

                    if (typ.Nazwisko.StartsWith(prefixWithSpace, StringComparison.OrdinalIgnoreCase))
                    {
                        typ.Prefiks = string.IsNullOrEmpty(typ.Prefiks)
                            ? pattern
                            : $"{typ.Prefiks} {pattern}";

                        typ.Nazwisko = typ.Nazwisko.Substring(prefixWithSpace.Length).Trim();
                        break;
                    }
                    else if (typ.Nazwisko.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        typ.Prefiks = string.IsNullOrEmpty(typ.Prefiks)
                            ? pattern
                            : $"{typ.Prefiks} {pattern}";

                        typ.Nazwisko = "";
                        break;
                    }
                }
            }

            // KROK 5: Wyodrębnienie tytułów
            (var Tytul1, typ.Imie) = TitleManager.SplitInitialTitle(typ.Imie);
            (var Tytul2, typ.Nazwisko) = TitleManager.SplitInitialTitle(typ.Nazwisko);

            tytulStr = $"{Tytul1} {Tytul2}".Trim().ToLower();
            tytulStr = TitleManager.NormalizeTitles(tytulStr);

            typ.Tytul = tytulStr;

            // KROK 6: Sprawdź czy nazwisko kończy się tekstem w cudzysłowach
            if (!string.IsNullOrEmpty(typ.Nazwisko))
            {
                var quotedPattern = @"\s*""([^""]+)""$";
                var match = Regex.Match(typ.Nazwisko, quotedPattern);

                if (match.Success)
                {
                    typ.Postfiks = match.Value.Trim();
                    typ.Nazwisko = typ.Nazwisko.Substring(0, match.Index).Trim();
                }
            }

            // KROK 7: Jeśli w nazwisku występuje minus, podziel na nazwisko i nazwisko2
            if (!string.IsNullOrEmpty(typ.Nazwisko) && typ.Nazwisko.Contains("-"))
            {
                var parts = typ.Nazwisko.Split(new[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2 && !parts[1].Trim().StartsWith("lecia"))
                {
                    typ.Nazwisko = parts[0].Trim();
                    typ.Nazwisko2 = parts[1].Trim();
                }
                else if (parts.Length == 1)
                {
                    typ.Nazwisko = parts[0].Trim();
                }
            }

            // KROK 8: Podziel imię na Imie i Imie2 jeśli zawiera spację
            if (!string.IsNullOrEmpty(typ.Imie) && typ.Imie.Contains(" "))
            {
                var parts = typ.Imie.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && char.IsLetter(parts[0][0]))
                {
                    typ.Imie = parts[0].Trim();
                    typ.Imie2 = parts[1].Trim();
                }
            }

            // SPRAWDZENIE IMIENIA
            if (string.IsNullOrEmpty(typ.Imie) && !string.IsNullOrEmpty(typ.Nazwisko) && typ.Nazwisko.Contains(" "))
            {
                var firstSpaceIndex = typ.Nazwisko.IndexOf(' ');

                if (firstSpaceIndex > 0)
                {
                    var propozycja = typ.Nazwisko.Substring(0, firstSpaceIndex).Trim();

                    if (IsImie(propozycja))
                    {
                        typ.Imie = propozycja;
                        typ.Nazwisko = typ.Nazwisko.Substring(firstSpaceIndex + 1).Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(tytulStr) && typ.Imie == "" && typ.Nazwisko != "" && typ.Postfiks == "" && !typ.Nazwisko.EndsWith("ego"))
            {
                typ.Postfiks = typ.Nazwisko;
                typ.Nazwisko = "";
            }

            return typ;
        }

        /// <summary>
        /// Sprawdza czy słowo to imię (używa słownika z AppData/Dictionaries/Imiona.txt)
        /// </summary>
        private bool IsImie(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;

            var normalized = UliceUtils.RemoveDiacritics(word.ToLowerInvariant());
            return ImionaSet.Contains(normalized);
        }
    }
   
    /// <summary>
    /// Wynik ładowania TerytUlicPoprawki
    /// </summary>
    public class LoadTerytUlicPoprawkiResult
    {
        public int TotalCount { get; set; }
        public int InsertedCount { get; set; }
        public int DeletedCount { get; set; }
    }

    /// <summary>
    /// Informacja o postępie ładowania
    /// </summary>
    public class LoadTerytUlicPoprawkiProgress
    {
        public string CurrentOperation { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public bool IsCompleted { get; set; }
    }
}