namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla procesu ³adowania kodów pocztowych, dziedzicz¹cy z GeneralLogger
    /// </summary>
    public class DiagnosticLogger : GeneralLogger
    {
        public DiagnosticLogger(string? appDataPath)
            : base(appDataPath, "DiagnosticLoader.txt", "Log diagnostyczny")
        {
        }
    }
}