using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;
using DbView;

namespace AddressLibrary.Models
{
    /// <summary>
    /// Model reprezentuj¹cy typy ulic osobowych z pe³n¹ dekompozycj¹ nazwy
    /// </summary>
    [TableParam(Choice = ChoiceMode.Huge, Description = "Poprawka nazwy ulicy z TERYT")]
    public class TerytUlicPoprawka
    {

        /// <summary>
        /// Identyfikator z Excela (klucz biznesowy - u¿ywany do wyszukiwania)
        /// Oryginalna pe³na nazwa ulicy: Cecha + Nazwa2 + Nazwa1
        /// Przyk³ad: "ul. Adama Mickiewicza", "al. Jana Paw³a II"
        /// </summary>
        [TableVisible(true)]
        [Required]
        [StringLength(500)]
        public string TerytId { get; set; } = string.Empty;

        /// <summary>
        /// Cecha ulicy (np. "ul.", "al.", "pl.")
        /// </summary>
        [TableVisible(true)]
        [StringLength(50)]
        public string? Cecha { get; set; }

        /// <summary>
        /// Prefiks (np. "p³k.", "gen.", "ks.", "im.", "imienia")
        /// </summary>
        [TableVisible(true)]
        [StringLength(50)]
        public string? Prefiks { get; set; }

        /// <summary>
        /// Tytu³ (np. "dr.", "prof.", "p³k.")
        /// </summary>
        [TableVisible(true)]
        [StringLength(50)]
        public string? Tytul { get; set; }

        /// <summary>
        /// Pierwsze imiê (np. "Stanis³awa")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Imie { get; set; }

        /// <summary>
        /// Drugie imiê (np. "Kamila" w "Krzysztofa Kamila Baczyñskiego")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Imie2 { get; set; }

        /// <summary>
        /// Pierwsze nazwisko (np. "Mickiewicza")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Nazwisko { get; set; }

        /// <summary>
        /// Drugie nazwisko (np. "Reymonta" w "W³adys³awa Stanis³awa Reymonta")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Nazwisko2 { get; set; }

        /// <summary>
        /// Pseudonim (np. "Zapory", "Zoœki", "Nila")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Pseudonim { get; set; }

        /// <summary>
        /// Postfiks/przydomek (np. dodatkowe informacje)
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        public string? Postfiks { get; set; }

        /// <summary>
        /// Identyfikator (klucz g³ówny w bazie danych - auto-increment)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }

    }
}