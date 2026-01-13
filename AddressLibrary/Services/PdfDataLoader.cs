using System.Globalization;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Models;
using AddressLibrary.Data;

namespace AddressLibrary.Services
{
    public class PdfDataLoader
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;

        public PdfDataLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
        }

        public async Task LoadDataFromPdfAsync(string pdfFilePath)
        {
            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException($"Plik PDF nie został znaleziony: {pdfFilePath}");
            }

            // Utwórz folder na logi
            var baseDir = _appDataPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var logsDir = Path.Combine(baseDir, "AppData", "Logs");
            
            // DIAGNOSTYKA: Utwórz katalog i zapisz ścieżkę
            Directory.CreateDirectory(logsDir);
            var logPath = Path.Combine(logsDir, "PdfLoad.txt");
            
            // DODAJ DIAGNOSTYKĘ - zapisz gdzie dokładnie jest log
            var diagnosticPath = Path.Combine(baseDir, "LOG_LOCATION.txt");
            await File.WriteAllTextAsync(diagnosticPath, 
                $"=== DIAGNOSTYKA LOKALIZACJI LOGÓW ==={Environment.NewLine}" +
                $"Data: {DateTime.Now}{Environment.NewLine}" +
                $"BaseDir (_appDataPath): {baseDir}{Environment.NewLine}" +
                $"LogsDir: {logsDir}{Environment.NewLine}" +
                $"PdfLoad.txt: {logPath}{Environment.NewLine}" +
                $"PdfProcess.txt: {Path.Combine(logsDir, "PdfProcess.txt")}{Environment.NewLine}" +
                $"Katalog istnieje: {Directory.Exists(logsDir)}{Environment.NewLine}");

            try
            {
                // Zapisz start logowania
                await File.WriteAllTextAsync(logPath, $"=== Ładowanie PDF - {DateTime.Now} ==={Environment.NewLine}{Environment.NewLine}");
                await File.AppendAllTextAsync(logPath, $"Plik: {pdfFilePath}{Environment.NewLine}");
                await File.AppendAllTextAsync(logPath, $"Lokalizacja logu: {logPath}{Environment.NewLine}{Environment.NewLine}");

                // Przetwórz PDF - PRZEKAŻ appDataPath!
                await File.AppendAllTextAsync(logPath, $"Rozpoczynam przetwarzanie PDF...{Environment.NewLine}");
                
                var records = PdfProcessor.Process(pdfFilePath, _appDataPath);

                await File.AppendAllTextAsync(logPath, $"Przetworzone rekordy: {records?.Count ?? 0}{Environment.NewLine}");

                if (records != null && records.Any())
                {
                    await File.AppendAllTextAsync(logPath, $"Dodawanie {records.Count} rekordów do bazy...{Environment.NewLine}");
                    
                    await _context.Pna.AddRangeAsync(records);
                    await _context.SaveChangesAsync();
                    
                    await File.AppendAllTextAsync(logPath, $"✅ Zakończono pomyślnie - dodano {records.Count} rekordów{Environment.NewLine}");
                }
                else
                {
                    await File.AppendAllTextAsync(logPath, "⚠️ Brak rekordów do dodania{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                await File.AppendAllTextAsync(logPath, $"{Environment.NewLine}❌ BŁĄD: {ex.Message}{Environment.NewLine}");
                await File.AppendAllTextAsync(logPath, $"Stack trace: {ex.StackTrace}{Environment.NewLine}");
                throw;
            }
        }
    }
}