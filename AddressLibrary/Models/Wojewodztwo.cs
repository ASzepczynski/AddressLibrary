using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Standard, Description = "Województwo")]
    public class Wojewodztwo
    {
        [Required]
        [MaxLength(100)]
        [MemberParam(Desc = "Nazwa województwa")]
        public string Nazwa { get; set; } = string.Empty;

        [Required]
        [MaxLength(2)]
        [MemberParam(Desc = "Kod TERYT województwa")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        // Relacja 1:N - jedno województwo ma wiele powiatów
        public ICollection<Powiat> Powiaty { get; set; } = new List<Powiat>();

        public string Opis() => Nazwa;
    }
}