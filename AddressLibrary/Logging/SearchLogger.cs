// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System;
using System.IO;
using System.Text;

namespace AddressLibrary.Logging
{
    public class SearchLogger : ILogger
    {
        private readonly string? _filePath;
        private readonly StringBuilder _currentSearchLog; // Bufor dla bieżącego wyszukiwania

        public string? LogFilePath => _filePath;
        public string? Name = "SearchLog";
        public string fileName;

        public SearchLogger(string? appDataPath)
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
                Console.WriteLine($"[{Name}] BŁĄD tworzenia katalogu logów: {ex.Message}");
            }

            _filePath = Path.Combine(logsDir, fileName);
            _currentSearchLog = new StringBuilder();
        }

        public virtual void Log(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            
            // ✅ Tylko do bufora (bez zapisu do pliku podczas masowej weryfikacji)
            _currentSearchLog.AppendLine(message);
        }

        public virtual void LogInfo(string message) => Log($"[INFO] {message}");
        public virtual void LogWarning(string message) => Log($"[WARN] {message}");
        public virtual void LogError(string message) => Log($"[ERROR] {message}");

        /// <summary>
        /// Zwraca log dla bieżącego wyszukiwania i czyści bufor
        /// </summary>
        public virtual string GetLog()
        {
            var log = _currentSearchLog.ToString();
            _currentSearchLog.Clear(); // Wyczyść dla następnego wyszukiwania
            return string.IsNullOrWhiteSpace(log) ? "(brak logu)" : log;
        }
    }
}
