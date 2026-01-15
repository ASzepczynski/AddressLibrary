using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Miasto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(7)]
        public string Symbol { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Nazwa { get; set; } = string.Empty;

        // Klucz obcy do gminy
        [Required]
        [ForeignKey(nameof(Gmina))]
        public int GminaId { get; set; }
        public Gmina Gmina { get; set; } = null!;

        // Klucz obcy do rodzaju miejscowoœci
        [Required]
        [ForeignKey(nameof(RodzajMiasta))]
        public int RodzajMiastaId { get; set; }
        public RodzajMiasta RodzajMiasta { get; set; } = null!;

        // Relacja 1:N - jedna miejscowoœæ ma wiele ulic
        public ICollection<Ulica> Ulice { get; set; } = new List<Ulica>();
        
        // Relacja 1:N - jedna miejscowoœæ ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();
    }
}