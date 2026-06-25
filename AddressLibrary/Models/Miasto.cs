using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Miejscowoœæ")]
    public class Miasto
    {

        [Required]
        [MaxLength(200)]
        [MemberParam(Desc = "Miejscowoœæ")]
        public string Nazwa { get; set; } = string.Empty;

        // Klucz obcy do gminy
        [TableVisible(false)]
        [Required]
        [ForeignKey(nameof(Gmina))]
        public int GminaId { get; set; }
        [TableVisible(true)]
        [Display(Name = "Gmina")]
        public Gmina Gmina { get; set; } = null!;

        // Klucz obcy do rodzaju miejscowoœci
        [TableVisible(false)]
        [Required]
        [ForeignKey(nameof(RodzajMiasta))]
        public int RodzajMiastaId { get; set; }
        [TableVisible(true)]
        [Display(Name = "Rodzaj miasta")]
        public RodzajMiasta RodzajMiasta { get; set; } = null!;

        [TableVisible(true)]
        [Required]
        [StringLength(7)]
        [Display(Name = "Kod TERYT")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }


        // Relacja 1:N - jedna miejscowoœæ ma wiele ulic
        public ICollection<Ulica> Ulice { get; set; } = new List<Ulica>();

        // Relacja 1:N - jedna miejscowoœæ ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();

        public string Opis() => $"{Nazwa} gm.{Gmina.Nazwa} pow.{Gmina.Powiat.Nazwa} woj.{Gmina.Powiat.Wojewodztwo.Nazwa}";
    }
}