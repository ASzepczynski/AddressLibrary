using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class KodPocztowy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        /// <summary>
        /// Kod pocztowy w formacie XX-XXX
        /// </summary>
        [Required]
        [MaxLength(6)]
        public string Kod { get; set; } = string.Empty;
        
        /// <summary>
        /// Numery domów obs³ugiwane przez ten kod pocztowy
        /// </summary>
        public string Numery { get; set; } = string.Empty;

        // Klucz obcy do miejscowoœci
        [Required]
        [ForeignKey(nameof(Miasto))]
        public int MiastoId { get; set; }
        public Miasto Miasto { get; set; } = null!;
//        public string Dzielnica { get; set; } = string.Empty;


        // Klucz obcy do ulicy (opcjonalny - niektóre kody dotycz¹ ca³ych miejscowoœci bez konkretnej ulicy)
        [ForeignKey(nameof(Ulica))]
        public int UlicaId { get; set; }
        public Ulica Ulica { get; set; }
    }
}