// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig.Logging;

namespace AddressLibrary.Logging
{
    public class DiagnosticLogger : ILogger
    {
        private readonly string? _filePath;

        public string? LogFilePath => _filePath;
        public string? Name = "DiagnosticLoader";
        public string fileName;

        public DiagnosticLogger(string? appDataPath)
        {
            
            fileName = Name + ".txt";
            var logsDir = Path.Combine(appDataPath ?? AppDomain.CurrentDomain.BaseDirectory, "AppData", "Logs");

            try
            {
                Directory.CreateDirectory(logsDir);
                Console.WriteLine($"[{Name}] Katalog logów: {logsDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] B£¥D tworzenia katalogu logów: {ex.Message}");
            }
            _filePath = Path.Combine(logsDir, fileName);
           try
            {
                Log("=== Log diagnostyczny ===");
                // Zapisz nag³ówek logu synchronicznie, jeœli plik nie istnieje
                Console.WriteLine($"Utworzono plik logu: {_filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B£¥D tworzenia pliku logu '{_filePath}': {ex.Message}");
            }
        }

        public virtual void Log(string message)
        {
            // _logBuffer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            File.AppendAllTextAsync(LogFilePath, message + "\r\n");
        }

        public virtual void LogInfo(string message) => Log("[INFO] " + message);
        public virtual void LogWarning(string message) => Log("[WARN] " + message);
        public virtual void LogError(string message) => Log("[ERROR] " + message);

        public string GetLog()
        {
            return "Na razie niezaimplementowane";
        }
    }
}
