using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DbView;

namespace AddressLibrary.Models
{
    [Display(Name = "Rodzaj gminy")]
    public class RodzajGminy
    {
        [Required]
        [StringLength(1)]
        [Display(Name = "Kod rodzaju gminy")]
        public string Kod { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Nazwa rodzaju gminy")]
        public string Nazwa { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "ID")]
        public int Id { get; set; }

        // Relacja 1:N - jeden rodzaj gminy mo¿e byæ przypisany do wielu gmin
        [TableVisible(false)]
        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
        
        public string Opis() => Nazwa;
    }
}