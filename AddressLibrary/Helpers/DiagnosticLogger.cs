// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    public class DiagnosticLogger : ILogger
    {
        private readonly string? _filePath;
        private readonly StringWriter _logBuffer = new();

        public string? LogFilePath => _filePath;
        public string? Name = "DiagnosticLoader";
        public string fileName;

        public DiagnosticLogger(string? appDataPath, string _name)
        {
            Name = _name;
            fileName = _name + ".txt";
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
            Console.WriteLine($"[KodyPocztoweLoader] Œcie¿ka logu: {_filePath}");
            try
            {
                _logBuffer.WriteLine("=== Log diagnostyczny ===");
                // Zapisz nag³ówek logu synchronicznie, jeœli plik nie istnieje
                if (!string.IsNullOrEmpty(_filePath) && !File.Exists(_filePath))
                {
                    File.WriteAllText(_filePath, _logBuffer.ToString());
                    _logBuffer.GetStringBuilder().Clear();
                }
                Console.WriteLine($"Utworzono plik logu: {_filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B£¥D tworzenia pliku logu '{_filePath}': {ex.Message}");
            }
        }

        public virtual void Log(string message)
        {
            _logBuffer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public virtual void LogInfo(string message) => Log("[INFO] " + message);
        public virtual void LogWarning(string message) => Log("[WARN] " + message);
        public virtual void LogError(string message) => Log("[ERROR] " + message);

        public async virtual Task FlushAsync()
        {
            if (!string.IsNullOrEmpty(_filePath))
            {
                await File.AppendAllTextAsync(_filePath, _logBuffer.ToString());
                _logBuffer.GetStringBuilder().Clear();
            }
        }
    }
}
