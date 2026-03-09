namespace AddressLibrary.Models
{
    /// <summary>
    /// Model reprezentuj¹cy typy ulic osobowych z pe³n¹ dekompozycj¹ nazwy
    /// </summary>
    public class TypUlicy
    {
        /// <summary>
        /// Identyfikator (klucz g³ówny)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Prefiks (np. "p³k.", "gen.", "ks.", "im.", "imienia")
        /// </summary>
        public string? Prefiks { get; set; }

        /// <summary>
        /// Tytu³ (np. "dr.", "prof.", "p³k.")
        /// </summary>
        public string? Tytul { get; set; }

        /// <summary>
        /// Pierwsze imiê (np. "Stanis³awa")
        /// </summary>
        public string? Imie { get; set; }

        /// <summary>
        /// Drugie imiê (np. "Kamila" w "Krzysztofa Kamila Baczyñskiego")
        /// </summary>
        public string? Imie2 { get; set; }

        /// <summary>
        /// Pierwsze nazwisko (np. "Mickiewicza")
        /// </summary>
        public string? Nazwisko { get; set; }

        /// <summary>
        /// Drugie nazwisko (np. "Reymonta" w "W³adys³awa Stanis³awa Reymonta")
        /// </summary>
        public string? Nazwisko2 { get; set; }

        /// <summary>
        /// Postfiks/przydomek (np. "Zapory" w "Hieronima Dekutowskiego Zapory", "Zoœki" w "Tadeusza Zawadzkiego Zoœki")
        /// </summary>
        public string? Postfiks { get; set; }

        /// <summary>
        /// Oryginalna pe³na nazwa ulicy: Cecha + Nazwa2 + Nazwa1
        /// Przyk³ad: "ul. Adama Mickiewicza", "al. Jana Paw³a II"
        /// </summary>
        public string? Original { get; set; }
    }
}