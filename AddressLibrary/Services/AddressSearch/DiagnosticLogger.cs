// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Logger diagnostyczny dla procesu wyszukiwania
    /// </summary>
    public class DiagnosticLogger
    {
        private readonly StringBuilder _log = new();

        public void Log(string message)
        {
            _log.AppendLine(message);
        }

        public void Log(string format, params object[] args)
        {
            _log.AppendLine(string.Format(format, args));
        }

        public string GetLog()
        {
            return _log.ToString();
        }

        public void Clear()
        {
            _log.Clear();
        }
    }
}