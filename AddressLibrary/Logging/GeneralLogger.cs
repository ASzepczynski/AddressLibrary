// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Typ loggera określający sposób zapisu logów
    /// </summary>
    public enum LoggerMode
    {
        /// <summary>Zapis bezpośrednio do pliku (thread-safe, z auto-flush)</summary>
        FileLog,
        
        /// <summary>Buforowany zapis (auto-flush co N wpisów)</summary>
        Buffered,
        
        /// <summary>Dummy logger - nie wykonuje żadnych operacji</summary>
        Dummy
    }

    /// <summary>
    /// Zarządza logowaniem procesu ładowania kodów pocztowych (thread-safe, StreamWriter)
    /// </summary>
    public class GeneralLogger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly string _logFileName;
        private readonly string _logTitle;
        private readonly LoggerMode _mode;
        private readonly StreamWriter? _writer;
        private readonly StringBuilder? _buffer;
        private readonly object _lock = new();
        private bool _disposed = false;
        
        // ✅ Dodane dla auto-flush w trybie Buffered
        private int _bufferedLineCount = 0;
        private const int MaxBufferedLines = 10000; // Auto-flush co 100 linii

        public string LogFilePath => _logFilePath;
        public LoggerMode Mode => _mode;

        public GeneralLogger(string? appDataPath, string? logFileName, string logTitle, LoggerMode mode = LoggerMode.FileLog)
        {
            _mode = mode;
            _logTitle = logTitle;
            _logFileName = logFileName ?? "log.txt";

            if (_mode == LoggerMode.Dummy)
            {
                // Dummy mode - nie inicjalizuj pliku ani bufora
                _logFilePath = string.Empty;
                return;
            }

            // Przygotuj ścieżkę do pliku
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

            _logFilePath = Path.Combine(logsDir, _logFileName);

            if (_mode == LoggerMode.FileLog)
            {
                // FileLog mode - otwórz plik ze StreamWriter
                _writer = new StreamWriter(_logFilePath, append: false)
                {
                    AutoFlush = true  // Auto-flush dla FileLog
                };
                _writer.WriteLine($"=== {logTitle} ===");
            }
            else if (_mode == LoggerMode.Buffered)
            {
                // Buffered mode - użyj StringBuilder
                _buffer = new StringBuilder();
                _buffer.AppendLine($"=== {logTitle} ===");
                _bufferedLineCount = 1;
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        private void WriteToBuffer(string message)
        {
            if (_buffer == null || _disposed)
                return;

            _buffer.AppendLine(message);
            _bufferedLineCount++;

            // ✅ Auto-flush co MaxBufferedLines linii
            if (_bufferedLineCount >= MaxBufferedLines)
            {
                FlushBufferToFile();
            }
        }

        private void FlushBufferToFile()
        {
            if (_buffer == null || _buffer.Length == 0)
                return;

            try
            {
                // Append do pliku (nie replace!)
                File.AppendAllText(_logFilePath, _buffer.ToString());
                _buffer.Clear();
                _bufferedLineCount = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeneralLogger] Błąd zapisu bufora: {ex.Message}");
            }
        }

        public void Log(string message)
        {
            if (_mode == LoggerMode.Dummy)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                if (_mode == LoggerMode.FileLog && _writer != null)
                {
                    _writer.WriteLine(message);
                }
                else if (_mode == LoggerMode.Buffered)
                {
                    WriteToBuffer(message);
                }
            }
        }

        public void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void LogWarning(string message)
        {
            Log($"[WARNING] {message}");
        }

        public void LogInfo(string message)
        {
            Log($"[INFO] {message}");
        }

        public string GetLog()
        {
            if (_mode == LoggerMode.Dummy)
                return string.Empty;

            lock (_lock)
            {
                if (_disposed)
                    return string.Empty;

                if (_mode == LoggerMode.FileLog && _writer != null)
                {
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
                else if (_mode == LoggerMode.Buffered)
                {
                    // Flush bufora + odczyt całego pliku
                    FlushBufferToFile();
                    
                    try
                    {
                        return File.ReadAllText(_logFilePath);
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }
                
                return string.Empty;
            }
        }

        public Task FlushAsync()
        {
            if (_mode == LoggerMode.Dummy)
                return Task.CompletedTask;

            lock (_lock)
            {
                if (_disposed)
                    return Task.CompletedTask;

                if (_mode == LoggerMode.FileLog && _writer != null)
                {
                    _writer.Flush();
                }
                else if (_mode == LoggerMode.Buffered)
                {
                    FlushBufferToFile();
                }
            }
            
            return Task.CompletedTask;
        }

        public async Task WriteSummaryAsync(string summary)
        {
            if (_mode == LoggerMode.Dummy)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                if (_mode == LoggerMode.FileLog && _writer != null)
                {
                    _writer.WriteLine(summary);
                    _writer.Flush();
                }
                else if (_mode == LoggerMode.Buffered)
                {
                    WriteToBuffer(summary);
                    FlushBufferToFile(); // Flush na końcu
                }
            }
            
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _mode != LoggerMode.Dummy)
                {
                    lock (_lock)
                    {
                        if (_mode == LoggerMode.FileLog)
                        {
                            _writer?.Flush();
                            _writer?.Dispose();
                        }
                        else if (_mode == LoggerMode.Buffered)
                        {
                            // Ostatni flush przy Dispose
                            FlushBufferToFile();
                        }
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
