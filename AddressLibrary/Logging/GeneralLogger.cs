// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Zarządza logowaniem procesu ładowania kodów pocztowych (thread-safe, StreamWriter)
    /// </summary>
    public class GeneralLogger : ILogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly string _logFileName;
        private readonly string _logTitle;
        private readonly StreamWriter _writer;
        private readonly object _lock = new();
        private bool _disposed = false;

        public string LogFilePath => _logFilePath;

        public GeneralLogger(string? appDataPath, string? logFileName, string logTitle)
        {
            string logsDir;
            
            if (!string.IsNullOrEmpty(appDataPath))
            {
                logsDir = Path.Combine(appDataPath, "AppData", "Logs");
            }
            else
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                logsDir = Path.Combine(baseDir, "..", "..", "..", "AppData", "Logs");
            }

            logsDir = Path.GetFullPath(logsDir);
            Directory.CreateDirectory(logsDir);

            _logTitle = logTitle;
            _logFileName = logFileName;
            _logFilePath = Path.Combine(logsDir, _logFileName);
            
            // ✅ Otwórz plik RAZ w konstruktorze (pozostanie otwarty do Dispose)
            _writer = new StreamWriter(_logFilePath, append: false)
            {
                AutoFlush = true  // ✅ Automatyczny flush po każdym WriteLine
            };
            
            _writer.WriteLine($"=== {logTitle} ===");
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public void Log(string message)
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.WriteLine(message);
            }
        }

        public void LogError(string message)
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.WriteLine($"[ERROR] {message}");
            }
        }

        public void LogWarning(string message)
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.WriteLine($"[WARNING] {message}");
            }
        }

        public void LogInfo(string message)
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.WriteLine($"[INFO] {message}");
            }
        }

        public string GetLog()
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.Flush();
                
                try
                {
                    return File.ReadAllText(_logFilePath);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public Task FlushAsync()
        {
            lock (_lock)
            {
                if (!_disposed)
                    _writer.Flush();
            }
            return Task.CompletedTask;
        }

        public async Task WriteSummaryAsync(string summary)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _writer.WriteLine(summary);
                    _writer.Flush();
                }
            }
            await Task.CompletedTask;
        }

        // ✅ Dispose pattern - zamknij StreamWriter w destruktorze
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        _writer?.Flush();
                        _writer?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        ~GeneralLogger()
        {
            Dispose(false);
        }
    }
}