namespace AddressLibrary.Models
{
    /// <summary>
    /// Reprezentuje adres z pliku Poprawki.txt
    /// </summary>
    public class Adres
    {
        /// <summary>
        /// Komentarz, zwykle komunikat o b³êdzie
        /// </summary>
        public string Komentarz { get; set; } = string.Empty;

        /// <summary>
        /// Identyfikator adresu
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Kraj (np. "Polska")
        /// </summary>
        public string Kraj { get; set; } = string.Empty;

        /// <summary>
        /// Kod pocztowy (format: XX-XXX)
        /// </summary>
        public string Kod { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa miasta/miejscowoœci
        /// </summary>
        public string Miasto { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa ulicy
        /// </summary>
        public string Ulica { get; set; } = string.Empty;

        /// <summary>
        /// Numer domu
        /// </summary>
        public string NrDomu { get; set; } = string.Empty;

        /// <summary>
        /// Numer lokalu/mieszkania
        /// </summary>
        public string NrLokalu { get; set; } = string.Empty;

        /// <summary>
        /// Województwo
        /// </summary>
        public string Wojewodztwo { get; set; } = string.Empty;

        /// <summary>
        /// Powiat
        /// </summary>
        public string Powiat { get; set; } = string.Empty;

        /// <summary>
        /// Gmina
        /// </summary>
        public string Gmina { get; set; } = string.Empty;
    }
}
