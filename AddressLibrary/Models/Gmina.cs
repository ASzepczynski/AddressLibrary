using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Gmina")]
    public class Gmina
    {
        [Required]
        [MaxLength(100)]
        [MemberParam(Desc = "Pe³na nazwa gminy")]
        public string Nazwa { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(Powiat))]
        [MemberParam(Desc = "Powiat, do którego nale¿y gmina")]
        public int PowiatId { get; set; }
        public Powiat Powiat { get; set; } = null!;

        [Required]
        [ForeignKey(nameof(RodzajGminy))]
        [MemberParam(Desc = "Rodzaj gminy")]
        public int RodzajGminyId { get; set; }
        public RodzajGminy RodzajGminy { get; set; } = null!;

        [Required]
        [MaxLength(7)]
        [MemberParam(Desc = "Kod TERYT gminy")]
        public string Kod { get; set; } = string.Empty;
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        public ICollection<Miasto> Miasta { get; set; } = new List<Miasto>();
        
        public string Opis() => $"{Nazwa} pow. {Powiat.Nazwa} woj. {Powiat.Wojewodztwo.Nazwa}";
    }
}