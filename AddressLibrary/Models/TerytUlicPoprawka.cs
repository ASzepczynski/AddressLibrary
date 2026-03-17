using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// Model reprezentuj¹cy typy ulic osobowych z pe³n¹ dekompozycj¹ nazwy
    /// </summary>
    public class TerytUlicPoprawka
    {
        /// <summary>
        /// Identyfikator (klucz g³ówny w bazie danych - auto-increment)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DbId { get; set; }

        /// <summary>
        /// Identyfikator z Excela (klucz biznesowy - u¿ywany do wyszukiwania)
        /// Oryginalna pe³na nazwa ulicy: Cecha + Nazwa2 + Nazwa1
        /// Przyk³ad: "ul. Adama Mickiewicza", "al. Jana Paw³a II"
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Cecha ulicy (np. "ul.", "al.", "pl.")
        /// </summary>
        public string? Cecha { get; set; }

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
        /// Pseudonim (np. "Zapory", "Zoœki", "Nila")
        /// </summary>
        public string? Pseudonim { get; set; }

        /// <summary>
        /// Postfiks/przydomek (np. dodatkowe informacje)
        /// </summary>
        public string? Postfiks { get; set; }
    }
}