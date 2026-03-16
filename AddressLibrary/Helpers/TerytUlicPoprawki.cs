using AddressLibrary.Models;
using AddressLibrary.Logging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Helpers
{
    public static class TerytUlicPoprawkiDictionary
    {
        /// <summary>
        /// Wczytuje słownik TypyUlic z pliku Excel
        /// Struktura kolumn:
        /// A = Id (pomijane)
        /// B = Prefiks
        /// C = Tytul
        /// D = Imie
        /// E = Imie2
        /// F = Nazwisko
        /// G = Nazwisko2
        /// H = Pseudonim
        /// I = Postfiks
        /// J = Original (klucz słownika)
        /// </summary>
        public static Dictionary<string, TerytUlicPoprawka> Load(string _appDataPath, GeneralLogger _logger)
        {
            var DictName = "TerytUlicPoprawki.xlsx";
            var dictionary = new Dictionary<string, TerytUlicPoprawka>(StringComparer.OrdinalIgnoreCase);
            var excelPath = Path.Combine(_appDataPath, "AppData", "Dictionaries", DictName);

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"⚠️ Plik {excelPath} nie istnieje");
                _logger.LogError($"Plik słownika nie istnieje: {excelPath}");
                return dictionary;
            }

            try
            {
                using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(excelPath, false))
                {
                    WorkbookPart? workbookPart = spreadsheet.WorkbookPart;
                    if (workbookPart == null)
                    {
                        Console.WriteLine("⚠️ Nie można otworzyć arkusza Excel");
                        return dictionary;
                    }

                    // ✅ Załaduj SharedStringTable raz na początku i skonwertuj do tablicy
                    string[] sharedStrings = Array.Empty<string>();
                    var sharedStringPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    if (sharedStringPart?.SharedStringTable != null)
                    {
                        sharedStrings = sharedStringPart.SharedStringTable
                            .Elements<SharedStringItem>()
                            .Select(item => item.InnerText)
                            .ToArray();
                    }

                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    bool isFirstRow = true;

                    // ✅ Iteruj bezpośrednio bez ToList() - oszczędność pamięci
                    foreach (var row in sheetData.Elements<Row>())
                    {
                        // Pomiń nagłówek
                        if (isFirstRow)
                        {
                            isFirstRow = false;
                            continue;
                        }

                        // ✅ Pobierz wszystkie komórki wiersza JEDNORAZOWO
                        var cellValues = GetRowCellsDictionary(row, sharedStrings);

                        // ✅ Original jest w kolumnie J (ostatnia)
                        var original = cellValues.GetValueOrDefault("J")?.Trim();

                        if (!string.IsNullOrWhiteSpace(original))
                        {
                            var terytUlicPoprawka = new TerytUlicPoprawka
                            {
                                // Id jest pomijane - będzie auto-generowane przez bazę danych
                                Prefiks = cellValues.GetValueOrDefault("B")?.Trim() ?? "",
                                Tytul = cellValues.GetValueOrDefault("C")?.Trim() ?? "",
                                Imie = cellValues.GetValueOrDefault("D")?.Trim() ?? "",
                                Imie2 = cellValues.GetValueOrDefault("E")?.Trim() ?? "",
                                Nazwisko = cellValues.GetValueOrDefault("F")?.Trim() ?? "",
                                Nazwisko2 = cellValues.GetValueOrDefault("G")?.Trim() ?? "",
                                Pseudonim = cellValues.GetValueOrDefault("H")?.Trim() ?? "",
                                Postfiks = cellValues.GetValueOrDefault("I")?.Trim() ?? "",
                                Original = original
                            };

                            // Klucz słownika to Original (kolumna J)
                            dictionary[original] = terytUlicPoprawka;
                        }
                    }
                }

                Console.WriteLine($"✓ Załadowano {dictionary.Count} wpisów ze słownika {DictName}");
                _logger.LogInfo($"Załadowano {dictionary.Count} wpisów ze słownika");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Błąd ładowania słownika TypyUlic: {ex.Message}");
                _logger.LogError($"Błąd ładowania słownika: {ex.Message}");
            }

            return dictionary;
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

            return new string(cellReference.Where(char.IsLetter).ToArray());
        }

        /// <summary>
        /// Pobiera wartość komórki z uwzględnieniem SharedStringTable
        /// </summary>
        private static string? GetCellValue(Cell cell, string[] sharedStrings)
        {
            if (cell.CellValue == null)
                return null;

            var value = cell.CellValue.InnerText;

            // Jeśli typ to SharedString, pobierz z tablicy
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out int stringIndex) && stringIndex < sharedStrings.Length)
                {
                    return sharedStrings[stringIndex];
                }
            }

            return value;
        }
    }
}
