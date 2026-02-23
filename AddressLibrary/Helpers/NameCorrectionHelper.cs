using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            // ✅ RETRY LOGIC - Próbuj otworzyć plik maksymalnie 3 razy
            const int maxRetries = 3;
            const int delayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"[NameCorrectionHelper] Próba {attempt}/{maxRetries} otwarcia pliku: {excelPath}");

                    // ✅ Otwórz w trybie READ-ONLY (FileShare.Read pozwala innym procesom czytać)
                    using var fileStream = new FileStream(
                        excelPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite); // ✅ Pozwól innym procesom na odczyt i zapis

                    using var document = SpreadsheetDocument.Open(fileStream, false);
                    var workbookPart = document.WorkbookPart;
                    var worksheetPart = workbookPart?.WorksheetParts.First();
                    var sheetData = worksheetPart?.Worksheet.Elements<SheetData>().First();

                    if (sheetData == null)
                    {
                        Console.WriteLine($"[NameCorrectionHelper] Brak danych w arkuszu");
                        return;
                    }

                    var rows = sheetData.Elements<Row>().Skip(1); // Pomiń nagłówek
                    int loadedCount = 0;

                    foreach (var row in rows)
                    {
                        var cells = row.Elements<Cell>().ToList();

                        if (cells.Count < 3)
                            continue;

                        var type = GetCellValue(workbookPart, cells[0]).Trim().ToUpperInvariant();
                        var oldName = GetCellValue(workbookPart, cells[1]).Trim();
                        var newName = GetCellValue(workbookPart, cells[2]).Trim();

                        // Walidacja typu
                        if (type != "M" && type != "U")
                            continue;

                        if (string.IsNullOrWhiteSpace(oldName))
                            continue;

                        // Dodaj do listy korekt dla danego typu
                        _correctionsByType[type].Add((oldName, newName));
                        loadedCount++;
                    }

                    Console.WriteLine($"[NameCorrectionHelper] ✓ Załadowano {loadedCount} korekt: M={_correctionsByType["M"].Count}, U={_correctionsByType["U"].Count}");
                    return; // ✅ Sukces - wyjdź z pętli retry
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    // ✅ Plik zajęty - spróbuj ponownie
                    Console.WriteLine($"[NameCorrectionHelper] ⚠️ Plik zajęty (próba {attempt}/{maxRetries}): {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Czekam {delayMs}ms przed kolejną próbą...");
                    Thread.Sleep(delayMs);
                }
                catch (IOException ex)
                {
                    // ✅ Ostatnia próba nie powiodła się
                    Console.WriteLine($"[NameCorrectionHelper] ✗ Nie udało się otworzyć pliku po {maxRetries} próbach: {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Kontynuacja bez korekt nazw.");
                    return;
                }
                catch (Exception ex)
                {
                    // ✅ Inny błąd (np. uszkodzony plik Excel)
                    Console.WriteLine($"[NameCorrectionHelper] ✗ Błąd wczytywania pliku Excel: {ex.Message}");
                    Console.WriteLine($"[NameCorrectionHelper] Kontynuacja bez korekt nazw.");
                    return;
                }
            }
        }

        /// <summary>
        /// Pobiera wartość komórki Excel (obsługuje SharedString)
        /// </summary>
        private string GetCellValue(WorkbookPart? workbookPart, Cell cell)
        {
            if (cell == null || cell.CellValue == null || workbookPart == null)
                return string.Empty;

            var value = cell.CellValue.InnerText;

            // Sprawdź czy to SharedString
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTable = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                if (stringTable != null)
                {
                    value = stringTable.SharedStringTable
                        .ElementAt(int.Parse(value))
                        .InnerText;
                }
            }

            return value ?? string.Empty;
        }

        /// <summary>
        /// Próbuje zastosować korekty nazwy - iteruje przez wszystkie korekty danego typu
        /// i wykonuje Replace dla każdej. Zwraca true jeśli nazwa się zmieniła.
        /// ✅ POPRAWKA: Używa szybkiego word boundary checking bez regex
        /// </summary>
        public bool TryCorrect(string type, string? oldName, out string? newName)
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

            // Iteruj przez wszystkie korekty danego typu
            foreach (var (oldPattern, newPattern) in _correctionsByType[normalizedType])
            {
                result = ReplaceWordIgnoreCase(result, oldPattern, newPattern);
            }

            newName = result;

            // Zwróć true tylko jeśli nazwa faktycznie się zmieniła
            return !string.Equals(oldName, result, StringComparison.Ordinal);
        }

        /// <summary>
        /// ✅ SZYBKA METODA: Zamienia wszystkie wystąpienia starego tekstu na nowy (case-insensitive)
        /// TYLKO gdy stary tekst występuje jako całe słowo (z granicami słów)
        /// Używa prostego porównywania znaków zamiast regex dla wydajności
        /// </summary>
        private static string ReplaceWordIgnoreCase(string text, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder(text.Length);
            int textIndex = 0;

            while (textIndex < text.Length)
            {
                // Znajdź następne wystąpienie wzorca (case-insensitive)
                int matchIndex = text.IndexOf(oldValue, textIndex, StringComparison.OrdinalIgnoreCase);

                if (matchIndex == -1)
                {
                    // Brak więcej dopasowań - skopiuj resztę tekstu
                    result.Append(text.AsSpan(textIndex));
                    break;
                }

                // Sprawdź granice słowa
                bool isWordStart = matchIndex == 0 || !IsLetter(text[matchIndex - 1]);
                bool isWordEnd = (matchIndex + oldValue.Length >= text.Length) || !IsLetter(text[matchIndex + oldValue.Length]);

                // Skopiuj tekst przed dopasowaniem
                result.Append(text.AsSpan(textIndex, matchIndex - textIndex));

                if (isWordStart && isWordEnd)
                {
                    // To całe słowo - zamień
                    result.Append(newValue);
                    textIndex = matchIndex + oldValue.Length;
                }
                else
                {
                    // To nie całe słowo - skopiuj oryginał i przejdź dalej
                    result.Append(text.AsSpan(matchIndex, oldValue.Length));
                    textIndex = matchIndex + oldValue.Length;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Sprawdza czy znak jest literą (w tym polskie znaki)
        /// </summary>
        private static bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == 'ą' || c == 'ć' || c == 'ę' || c == 'ł' || c == 'ń' || c == 'ó' || c == 'ś' || c == 'ź' || c == 'ż' ||
                   c == 'Ą' || c == 'Ć' || c == 'Ę' || c == 'Ł' || c == 'Ń' || c == 'Ó' || c == 'Ś' || c == 'Ź' || c == 'Ż';
        }

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