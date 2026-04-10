using AddressLibrary.Logging;

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla wyszukiwania pojedynczego kodu pocztowego (strona Szukaj).
    /// Zapisuje do AppData/Logs/Szukaj.txt.
    /// </summary>
    public class SzukajLogger : GeneralLogger
    {
        public SzukajLogger(string? appDataPath)
            : base(appDataPath, "Szukaj.txt", "Log wyszukiwania kodów pocztowych", LoggerMode.Buffered)
        {
        }
    }
}
