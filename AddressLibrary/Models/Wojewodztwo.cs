using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;
using DbView;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Standard, Description = "Województwo")]
    public class Wojewodztwo
    {
        [TableVisible(true)]
        [Required]
        [StringLength(100)]
        [Display(Name = "Nazwa województwa")]
        public string Nazwa { get; set; } = string.Empty;

        [TableVisible(true)]
        [Required]
        [StringLength(2)]
        [Display(Name = "Kod TERYT województwa")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }

        // Relacja 1:N - jedno województwo ma wiele powiatów
        public ICollection<Powiat> Powiaty { get; set; } = new List<Powiat>();

        public string Opis() => Nazwa;
    }
}