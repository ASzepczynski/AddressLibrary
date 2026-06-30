using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// Reprezentuje urz¹d skarbowy
    /// </summary>
    [Display(Name = "Urz¹d skarbowy")]
    public class UrzadSkarbowy
    {
        /// <summary>
        /// Nazwa urzêdu skarbowego
        /// </summary>
        [TableVisible(true)]
        [Required]
        [StringLength(200)]
        [Display(Name = "Nazwa")]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Kod pocztowy
        /// </summary>
        [TableVisible(true)]
        [StringLength(10)]
        [Display(Name = "Kod")]
        public string Kod { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa miasta
        /// </summary>
        [TableVisible(true)]
        [StringLength(100)]
        [Display(Name = "Miasto")]
        public string Miasto { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa ulicy
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Ulica")]
        public string Ulica { get; set; } = string.Empty;

        /// <summary>
        /// Numer domu
        /// </summary>
        [TableVisible(true)]
        [StringLength(20)]
        [Display(Name = "Nr domu")]
        public string NrDomu { get; set; }= string.Empty;

        /// <summary>
        /// Klucz obcy do Ulicy (wype³niany automatycznie podczas importu)
        /// </summary>
        [TableVisible(false)]
        [ForeignKey(nameof(UlicaNavigation))]
        public int UlicaId { get; set; }

        /// <summary>
        /// Nawigacja do obiektu Ulica
        /// </summary>
        [TableVisible(true)]
        [Display(Name = "Ulica (nawigacja)")]
        public Ulica? UlicaNavigation { get; set; }

        /// <summary>
        /// Adres email
        /// </summary>
        [TableVisible(true)]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Strona WWW
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Www")]
        public string Www { get; set; } = string.Empty;

        /// <summary>
        /// Strona WWW
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Zasiêg")]
        public string Zasieg { get; set; } = string.Empty;

        /// <summary>
        /// Identyfikator urzêdu
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }

        public string Opis() => $"{Nazwa}";

    }
}
