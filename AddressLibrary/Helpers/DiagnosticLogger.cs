using AddressLibrary.Logging;
using System.Text;

namespace AddressLibrary.Helpers
{
    public class DiagnosticLogger : ILogger
    {
        private readonly StringBuilder _log = new();

        public void Log(string message)
        {
            _log.AppendLine(message);
        }

        public void LogInfo(string message) => Log("[INFO] " + message);
        public void LogWarning(string message) => Log("[WARN] " + message);
        public void LogError(string message) => Log("[ERROR] " + message);

        public string GetLog() => _log.ToString();
    }
}