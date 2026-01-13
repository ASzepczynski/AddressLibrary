using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using CsvHelper;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using AddressLibrary.Models;

public static partial class PdfProcessor
{
    public static List<Pna> Process(string inputPdf, string? appDataPath = null)
    {
        // Utwórz log
        var logsDir = Path.Combine(appDataPath ?? AppDomain.CurrentDomain.BaseDirectory, "AppData", "Logs");
        Directory.CreateDirectory(logsDir);
        var logPath = Path.Combine(logsDir, "PdfProcess.txt");
        
        // Inicjalizuj log
        File.WriteAllText(logPath, $"=== Przetwarzanie PDF - {DateTime.Now} ==={Environment.NewLine}{Environment.NewLine}");
        File.AppendAllText(logPath, $"Plik wejściowy: {inputPdf}{Environment.NewLine}");
        
        Regex PostalCodeRegex = new Regex("^^\\d{2}-\\d{3} \\p{Lu}", RegexOptions.Compiled);
        
        try
        {
            File.AppendAllText(logPath, $"Otwieranie pliku PDF...{Environment.NewLine}");
            using var reader = PdfDocument.Open(inputPdf);
            File.AppendAllText(logPath, $"✅ PDF otwarty. Liczba stron: {reader.NumberOfPages}{Environment.NewLine}");
            
            var cp1250 = Encoding.GetEncoding(1250);
            string outputCsv = Path.ChangeExtension(inputPdf, ".csv");
            File.AppendAllText(logPath, $"Plik wyjściowy CSV: {outputCsv}{Environment.NewLine}{Environment.NewLine}");
            
            using var writer = new StreamWriter(outputCsv, false, cp1250);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            string Delimiter = ";";
            var records = new List<Pna>();

            int pageNum = 0;
            int totalWords = 0;
            int totalTokens = 0;
            
            foreach (Page page in reader.GetPages())
            {
                pageNum++;
                File.AppendAllText(logPath, $"--- Strona {pageNum}/{reader.NumberOfPages} ---{Environment.NewLine}");
                
                var words = page.GetWords().ToList();
                totalWords += words.Count;
                File.AppendAllText(logPath, $"  Słów na stronie: {words.Count}{Environment.NewLine}");
                
                if (words.Count == 0)
                {
                    File.AppendAllText(logPath, $"  ⚠️ Brak słów - fallback do raw text{Environment.NewLine}");
                    // fallback to raw text if no words
                    var rawLines = page.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rawLine in rawLines)
                    {
                        var normalized = Regex.Replace(rawLine.Trim(), "\\s{2,}", Delimiter);
                        var cols = normalized.Split(Delimiter);
                        foreach (var col in cols)
                        {
                            csv.WriteField(col.Trim());
                        }
                        csv.NextRecord();
                    }
                    continue;
                }

                var tokens = new List<string>();
                if (words.Count > 0)
                {
                    var sb = new StringBuilder(words[0].Text);
                    var prev = words[0];
                    for (int i = 1; i < words.Count; i++)
                    {
                        var w = words[i];
                        if (AreSameToken(prev, w))
                        {
                            sb.Append(' ').Append(w.Text);
                        }
                        else
                        {
                            tokens.Add(sb.ToString());
                            sb.Clear();
                            sb.Append(w.Text);
                        }
                        prev = w;
                    }

                    tokens.Add(sb.ToString());
                }

                totalTokens += tokens.Count;
                File.AppendAllText(logPath, $"  Tokenów na stronie: {tokens.Count}{Environment.NewLine}");

                var slowa = new List<string>();
                bool bPoczatek = true;
                int recordsOnPage = 0;

                for (int ti = 0; ti < tokens.Count; ti++)
                {
                    var token = tokens[ti];

                    var bKod = PostalCodeRegex.IsMatch(token);

                    if (bPoczatek && !bKod)
                    {
                        // Pomijaj początkowe słowa aż do znalezienia kodu pocztowego
                        continue;
                    }
                    bPoczatek = false;
                    if (bKod && slowa.Count > 0)
                    {
                        // Nowa linia zaczyna się od kodu pocztowego, więc zapisujemy poprzednią linię
                        bool koniec = WyprowadzDane(slowa, records, Delimiter);
                        recordsOnPage++;
                        slowa.Clear();
                        if (koniec)
                        {
                            File.AppendAllText(logPath, $"  🛑 Zatrzymano przetwarzanie (kod 00-940){Environment.NewLine}");
                            break;
                        }
                    }
                    slowa.Add(token);
                }
                
                // Jeśli coś zostało
                if (slowa.Count > 0)
                {
                    bool koniec2 = WyprowadzDane(slowa, records, Delimiter);
                    recordsOnPage++;
                    slowa.Clear();
                    if (koniec2)
                    {
                        File.AppendAllText(logPath, $"  🛑 Zatrzymano przetwarzanie (kod 00-940){Environment.NewLine}");
                        break;
                    }
                }
                
                File.AppendAllText(logPath, $"  Rekordów dodanych na stronie: {recordsOnPage}{Environment.NewLine}");
                File.AppendAllText(logPath, $"  Łącznie rekordów do tej pory: {records.Count}{Environment.NewLine}{Environment.NewLine}");
            }

            File.AppendAllText(logPath, $"{Environment.NewLine}=== Podsumowanie ==={Environment.NewLine}");
            File.AppendAllText(logPath, $"Przetworzone strony: {pageNum}{Environment.NewLine}");
            File.AppendAllText(logPath, $"Łączna liczba słów: {totalWords}{Environment.NewLine}");
            File.AppendAllText(logPath, $"Łączna liczba tokenów: {totalTokens}{Environment.NewLine}");
            File.AppendAllText(logPath, $"Utworzonych rekordów: {records.Count}{Environment.NewLine}{Environment.NewLine}");

            // write header
            writer.Write($"Kod{Delimiter}");
            writer.Write($"Miasto{Delimiter}");
            writer.Write($"Dzielnica{Delimiter}");
            writer.Write($"Ulica{Delimiter}");
            writer.Write($"Gmina{Delimiter}");
            writer.Write($"Powiat{Delimiter}");
            writer.Write($"Wojewodztwo{Delimiter}");
            writer.Write($"Numery");
            writer.WriteLine();

            // write collected records
            File.AppendAllText(logPath, $"Zapisywanie do CSV...{Environment.NewLine}");
            foreach (var r in records)
            {
                var line = $"{r.Kod}{Delimiter}{r.Miasto}{Delimiter}{r.Dzielnica}{Delimiter}{r.Ulica}{Delimiter}{r.Gmina}{Delimiter}{r.Powiat}{Delimiter}{r.Wojewodztwo}{Delimiter}{r.Numery}";
                writer.WriteLine(line);
            }
            
            File.AppendAllText(logPath, $"✅ Zakończono pomyślnie - wygenerowano plik: {outputCsv}{Environment.NewLine}");
            
            return records;
        }
        catch (Exception ex)
        {
            File.AppendAllText(logPath, $"{Environment.NewLine}❌ BŁĄD: {ex.Message}{Environment.NewLine}");
            File.AppendAllText(logPath, $"Stack trace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
            throw;
        }
    }
}