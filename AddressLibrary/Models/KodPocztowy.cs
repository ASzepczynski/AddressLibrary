using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [Display(Name = "Kod pocztowy")]
    public class KodPocztowy
    {

        /// <summary>
        /// Kod pocztowy w formacie XX-XXX
        /// </summary>
        [TableVisible(true)]
        [Required]
        [StringLength(6)]
        [Display(Name = "Kod pocztowy")]
        public string Kod { get; set; } = string.Empty;

        /// <summary>
        /// Numery domów obs³ugiwane przez ten kod pocztowy
        /// </summary>
        [Display(Name = "Numery domów")]
        public string Numery { get; set; } = string.Empty;

        // Klucz obcy do miejscowoœci
        [TableVisible(false)]
        [Required]
        [ForeignKey(nameof(Miasto))]
        public int MiastoId { get; set; }
        [TableVisible(true)]
        [Display(Name = "Miasto")]
        public Miasto Miasto { get; set; } = null!;
        //        public string Dzielnica { get; set; } = string.Empty;


        // Klucz obcy do ulicy (opcjonalny - niektóre kody dotycz¹ ca³ych miejscowoœci bez konkretnej ulicy)
        [TableVisible(false)]
        [ForeignKey(nameof(Ulica))]
        public int UlicaId { get; set; }
        [TableVisible(true)]
        [Display(Name = "Ulica")]
        public Ulica? Ulica { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }

    }
}
