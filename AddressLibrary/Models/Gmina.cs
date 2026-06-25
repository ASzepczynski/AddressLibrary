using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [Display(Name = "Gminy")]
    public class Gmina
    {
        [TableVisible(true)]
        [Required]
        [StringLength(100)]
        [Display(Name = "Pe³na nazwa gminy")]
        public string Nazwa { get; set; } = string.Empty;

        [TableVisible(false)]
        public int PowiatId { get; set; }

        [TableVisible(true)]
        [Required]
        [Display(Name = "Powiat do którego nale¿y gmina")]
        public Powiat Powiat { get; set; } = null!;

        [TableVisible(false)]
        [Required]
        [ForeignKey(nameof(RodzajGminy))]
        public int RodzajGminyId { get; set; }

        [TableVisible(true)]
        [Display(Name = "Rodzaj gminy")]
        public RodzajGminy RodzajGminy { get; set; } = null!;

        [TableVisible(true)]
        [Required]
        [StringLength(7)]
        [Display(Name = "Kod TERYT gminy")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
        public int Id { get; set; }

        public ICollection<Miasto> Miasta { get; set; } = new List<Miasto>();
        
        public string Opis() => $"{Nazwa} pow. {Powiat.Nazwa} woj. {Powiat.Wojewodztwo.Nazwa}";
    }
}