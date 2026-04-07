using AddressLibrary.Attributes;
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
        [Required]
        [ForeignKey(nameof(Gmina))]
        [MemberParam(Desc = "Gmina")]
        public int GminaId { get; set; }
        public Gmina Gmina { get; set; } = null!;

        // Klucz obcy do rodzaju miejscowoœci
        [Required]
        [ForeignKey(nameof(RodzajMiasta))]
        [MemberParam(Desc = "Rodzaj miasta")]
        public int RodzajMiastaId { get; set; }
        public RodzajMiasta RodzajMiasta { get; set; } = null!;

        [Required]
        [MaxLength(7)]
        [MemberParam(Desc = "Kod Teryt")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID Miasta")]
        public int Id { get; set; }


        // Relacja 1:N - jedna miejscowoœæ ma wiele ulic
        public ICollection<Ulica> Ulice { get; set; } = new List<Ulica>();

        // Relacja 1:N - jedna miejscowoœæ ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();

        public string Opis() => $"{Nazwa} gm.{Gmina.Nazwa} pow.{Gmina.Powiat.Nazwa} woj.{Gmina.Powiat.Wojewodztwo.Nazwa}";
    }
}