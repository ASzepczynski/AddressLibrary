namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla zmian prefiksów ulic (gdy prefix jest w Nazwa1 zamiast w Cecha)
    /// </summary>
    public class PrefixChangeLogger : GeneralLogger
    {
        public PrefixChangeLogger(string? appDataPath)
            : base(appDataPath, "ZmianyPrefiksu.txt", "Log zmian prefiksów ulic", LoggerMode.Buffered)
        {
        }

        /// <summary>
        /// Loguje zmianê prefiksu
        /// </summary>
        public void LogPrefixChange(string oldCecha, string oldNazwa1, string newCecha, string newNazwa1, string miasto)
        {
            var message = $"Zmiana: [{oldCecha}] '{oldNazwa1}' -> [{newCecha}] '{newNazwa1}' | Miasto: {miasto}";
            Log(message);
        }
    }
}