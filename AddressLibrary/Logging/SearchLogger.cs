namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla procesu ładowania kodów pocztowych, dziedziczący z GeneralLogger
    /// </summary>
    public class SearchLogger : GeneralLogger
    {
        public SearchLogger(string? appDataPath)
            : base(appDataPath, "VerifyStreet.txt", "Log weryfikacji ulic", LoggerMode.Buffered)
        {
        }
    }
}