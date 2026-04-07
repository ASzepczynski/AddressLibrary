using AddressLibrary.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Kod pocztowy")]
    public class KodPocztowy
    {

        /// <summary>
        /// Kod pocztowy w formacie XX-XXX
        /// </summary>
        [Required]
        [MaxLength(6)]
        [MemberParam(Desc = "Kod pocztowy")]
        public string Kod { get; set; } = string.Empty;

        /// <summary>
        /// Numery domów obs³ugiwane przez ten kod pocztowy
        /// </summary>
        [MemberParam(Desc = "Numery domów")]
        public string Numery { get; set; } = string.Empty;

        // Klucz obcy do miejscowoœci
        [Required]
        [ForeignKey(nameof(Miasto))]
        [MemberParam(Desc = "Miasto")]
        public int MiastoId { get; set; }
        public Miasto Miasto { get; set; } = null!;
        //        public string Dzielnica { get; set; } = string.Empty;


        // Klucz obcy do ulicy (opcjonalny - niektóre kody dotycz¹ ca³ych miejscowoœci bez konkretnej ulicy)
        [ForeignKey(nameof(Ulica))]
        [MemberParam(Desc = "Ulica")]
        public int UlicaId { get; set; }
        public Ulica? Ulica { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

    }
}
