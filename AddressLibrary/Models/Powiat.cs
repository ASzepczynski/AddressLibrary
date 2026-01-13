using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Powiat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(4)]
        public string Kod { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Nazwa { get; set; } = string.Empty;

        // Klucz obcy do województwa
        [Required]
        [ForeignKey(nameof(Wojewodztwo))]
        public int WojewodztwoId { get; set; }
        public Wojewodztwo Wojewodztwo { get; set; } = null!;

        // Relacja 1:N - jeden powiat ma wiele gmin
        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
    }
}