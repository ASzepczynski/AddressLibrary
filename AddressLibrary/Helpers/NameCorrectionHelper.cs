using AddressLibrary.Models;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Helper do wczytywania i stosowania korekt nazw miast i ulic z pliku Excel (AppData/Updates/KorektyNazw.xlsx)
    /// Format pliku: Typ | Stara nazwa | Nowa nazwa
    /// Typ: M (miasto), U (ulica)
    /// </summary>
    public class NameCorrectionHelper
    {
        private readonly Dictionary<string, List<(string OldName, string NewName)>> _correctionsByType;

        public NameCorrectionHelper(string appDataPath)
        {
            _correctionsByType = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase)
            {
                { "M", new List<(string, string)>() },
                { "U", new List<(string, string)>() }
            };

            LoadFromExcel(appDataPath);
        }

        /// <summary>
        /// Wczytuje korekty z pliku Excel z obsługą błędów (plik zajęty, brak dostępu)
        /// </summary>
        private void LoadFromExcel(string appDataPath)
        {
            var excelPath = Path.Combine(appDataPath, "AppData", "Updates", "KorektyNazw.xlsx");

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"[NameCorrectionHelper] Plik korekt nie istnieje: {excelPath}");
                return;
            }

            const int maxRetries = 3;
            const int delayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"[NameCorrectionHelper] Próba {attempt}/{maxRetries} otwarcia pliku: {excelPath}");

                    var rows = ExcelTableReader.Read(excelPath);
                    int loadedCount = 0;

                    foreach (var row in rows)
                    {
                        var type    = row.GetString("Typ").Trim().ToUpperInvariant();
                        var oldName = row.GetString("Stara nazwa").Trim().Replace("*", " ");
                        var newName = row.GetString("Nowa nazwa").Trim().Replace("*", " ");

                        if (type != "M" && type != "U")
                            continue;

                        if (string.IsNullOrWhiteSpace(oldName))
                            continue;

                        _correctionsByType[type].Add((oldName, newName));
                        loadedCount++;
                    }

                    Console.WriteLine($"[NameCorrectionHelper] ✓ Załadowano {loadedCount} korekt: M={_correctionsByType["M"].Count}, U={_correctionsByType["U"].Count}");
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"[NameCorrectionHelper] ⚠️ Plik zajęty (próba {attempt}/{maxRetries}): {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Czekam {delayMs}ms przed kolejną próbą...");
                    Thread.Sleep(delayMs);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[NameCorrectionHelper] ✗ Nie udało się otworzyć pliku po {maxRetries} próbach: {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Kontynuacja bez korekt nazw.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NameCorrectionHelper] ✗ Błąd wczytywania pliku Excel: {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Kontynuacja bez korekt nazw.");
                    return;
                }
            }
        }

        /// <summary>
        /// Próbuje zastosować korekty nazwy - iteruje przez wszystkie korekty danego typu
        /// i wykonuje Replace dla każdej. Zwraca true jeśli nazwa się zmieniła.
        /// ✅ POPRAWKA: Obsługuje dwa tryby - dosłowny (z !) i word boundary (bez !)
        /// </summary>
        public bool TryCorrect(string type, string? oldName, out string newName)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(oldName))
            {
                newName = oldName ?? string.Empty;
                return false;
            }

            var normalizedType = type.Trim().ToUpperInvariant();

            // Sprawdź czy typ jest obsługiwany
            if (!_correctionsByType.ContainsKey(normalizedType))
            {
                newName = oldName;
                return false;
            }

            var result = oldName;
            bool hasChanged;


            // ✅ POPRAWKA: Iteruj dopóki następna iteracja wprowadza zmiany
            int LiczbaPoprawek = 0;
            do
            {
                var przed = result;
                
                // Iteruj przez wszystkie korekty danego typu
                foreach (var (oldPattern, newPattern) in _correctionsByType[normalizedType])
                {
                    // ✅ NOWA LOGIKA: Sprawdź czy oldPattern zaczyna się od "!"
                    if (oldPattern.Contains("agore") && newPattern.Contains("agore") && result.Contains("agore"))
                    {
                        int y = 1;
                    }

                    if (oldPattern.StartsWith("!"))
                    {
                        // Tryb dosłowny - usuń "!" i zamień dokładnie
                        var patternBezWykrzyknika = oldPattern.Substring(1);
                        result = ReplaceMaster.ReplaceStringIgnoreCase(result, patternBezWykrzyknika, newPattern);
                    }
                    else
                    {
                        // Tryb word boundary - zamień tylko całe słowa
                        result = ReplaceMaster.ReplaceWordIgnoreCase(result, oldPattern, newPattern);
                    }
                }

                // ✅ POPRAWKA: hasChanged == true gdy stringi są RÓŻNE (była zmiana)
                hasChanged = !string.Equals(przed, result, StringComparison.Ordinal);
                LiczbaPoprawek++;
                
            } while (hasChanged && LiczbaPoprawek < 5); // ✅ Kontynuuj gdy była zmiana (może być kolejna)

            if (LiczbaPoprawek >= 5)
            {
                throw new Exception($"Liczba poprawek przekroczyła 5 - przypuszczalnie nieskończona pętla '{oldName}/{result}'");
            }

            result = ReplaceMaster.ReplaceStringIgnoreCase(result, "\"", "");

            newName = result;

            // Zwróć true tylko jeśli nazwa faktycznie się zmieniła
            return !string.Equals(oldName, newName, StringComparison.Ordinal);
        }


        /// <summary>
        /// Sprawdza czy znak jest granicą słowa (spacja, kropka, przecinek, myślnik, itp.)
        /// </summary>
       

        public int Count => _correctionsByType.Values.Sum(list => list.Count);

        public int GetCountByType(string type)
        {
            var normalizedType = type.Trim().ToUpperInvariant();
            return _correctionsByType.ContainsKey(normalizedType)
                ? _correctionsByType[normalizedType].Count
                : 0;
        }
    }
}