using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DbView;

namespace AddressLibrary.Models
{
    [Display(Name = "Powiat")]
    public class Powiat
    {

        [Required]
        [StringLength(100)]
        [Display(Name = "Nazwa powiatu")]
        public string Nazwa { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(Wojewodztwo))]
        [Display(Name = "Województwo, do którego nale¿y powiat")]
        public int WojewodztwoId { get; set; }
        public Wojewodztwo Wojewodztwo { get; set; } = null!;

        [Required]
        [StringLength(4)]
        [Display(Name = "Kod TERYT powiatu")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "ID")]
        public int Id { get; set; }

        [TableVisible(false)]
        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
        
        public string Opis() => $"{Nazwa} woj.{Wojewodztwo.Opis()}";
    }
}