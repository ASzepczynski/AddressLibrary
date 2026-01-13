// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using System.Text;

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Zarz¹dza logowaniem procesu ³adowania kodów pocztowych
    /// </summary>
    internal class LoadLogger
    {
        private readonly string _logFilePath;
        private readonly StringBuilder _logBuffer = new();

        public string LogFilePath => _logFilePath;

        public LoadLogger(string? appDataPath)
        {
            var logsDir = Path.Combine(appDataPath ?? AppDomain.CurrentDomain.BaseDirectory, "AppData", "Logs");

            try
            {
                Directory.CreateDirectory(logsDir);
                Console.WriteLine($"[KodyPocztoweLoader] Katalog logów: {logsDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KodyPocztoweLoader] B£¥D tworzenia katalogu logów: {ex.Message}");
            }

            _logFilePath = Path.Combine(logsDir, "LoadLog.txt");
            Console.WriteLine($"[KodyPocztoweLoader] Œcie¿ka logu: {_logFilePath}");
        }

        public async Task InitializeAsync()
        {
            try
            {
                await File.WriteAllTextAsync(_logFilePath, 
                    $"=== Log ³adowania kodów pocztowych - {DateTime.Now} ==={Environment.NewLine}{Environment.NewLine}");
                Console.WriteLine($"[KodyPocztoweLoader] Utworzono plik logu: {_logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KodyPocztoweLoader] B£¥D tworzenia pliku logu: {ex.Message}");
            }
        }

        public void LogError(string message)
        {
            _logBuffer.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        public async Task FlushAsync()
        {
            if (_logBuffer.Length > 0)
            {
                try
                {
                    await File.AppendAllTextAsync(_logFilePath, _logBuffer.ToString());
                    _logBuffer.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KodyPocztoweLoader] B£¥D zapisu bufora logu: {ex.Message}");
                }
            }
        }

        public async Task WriteSummaryAsync(string summary)
        {
            try
            {
                await File.AppendAllTextAsync(_logFilePath, summary);
                Console.WriteLine($"[KodyPocztoweLoader] Zapisano podsumowanie do logu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KodyPocztoweLoader] B£¥D zapisu podsumowania: {ex.Message}");
            }
        }
    }
}