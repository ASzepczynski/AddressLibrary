using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// Reprezentuje urz¹d skarbowy
    /// </summary>
    public class UrzadSkarbowy
    {
        /// <summary>
        /// Identyfikator urzêdu
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Nazwa urzêdu skarbowego
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Kod pocztowy
        /// </summary>
        [MaxLength(10)]
        public string? Kod { get; set; }

        /// <summary>
        /// Nazwa miasta
        /// </summary>
        [MaxLength(100)]
        public string? Miasto { get; set; }

        /// <summary>
        /// Nazwa ulicy
        /// </summary>
        [MaxLength(200)]
        public string? Ulica { get; set; }

        /// <summary>
        /// Numer domu
        /// </summary>
        [MaxLength(20)]
        public string? NrDomu { get; set; }

        /// <summary>
        /// Klucz obcy do Ulicy (wype³niany automatycznie podczas importu)
        /// </summary>
        [ForeignKey(nameof(Models.Ulica))]
        public int? UlicaId { get; set; }

        /// <summary>
        /// Nawigacja do obiektu Ulica
        /// </summary>
        public Ulica? UlicaNavigation { get; set; }

        /// <summary>
        /// Adres email
        /// </summary>
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// Strona WWW
        /// </summary>
        [MaxLength(200)]
        public string? Www { get; set; }
    }
}