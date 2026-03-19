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
        public string? Kraj { get; set; }

        /// <summary>
        /// Kod pocztowy (format: XX-XXX)
        /// </summary>
        public string? Kod { get; set; }

        /// <summary>
        /// Nazwa miasta/miejscowoœci
        /// </summary>
        public string? Miasto { get; set; }

        /// <summary>
        /// Nazwa ulicy
        /// </summary>
        public string? Ulica { get; set; }

        /// <summary>
        /// Numer domu
        /// </summary>
        public string? NrDomu { get; set; }

        /// <summary>
        /// Numer lokalu/mieszkania
        /// </summary>
        public string? NrLokalu { get; set; }

        /// <summary>
        /// Województwo
        /// </summary>
        public string? Wojewodztwo { get; set; }

        /// <summary>
        /// Powiat
        /// </summary>
        public string? Powiat { get; set; }

        /// <summary>
        /// Gmina
        /// </summary>
        public string? Gmina { get; set; }
    }
}
