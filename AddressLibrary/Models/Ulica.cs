using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Ulica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string? Cecha { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Nazwa1 { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Nazwa2 { get; set; }

        // ✅ DODANO: Pole dzielnica
        [MaxLength(200)]
        public string? Dzielnica { get; set; }

        // Klucz obcy do miejscowości
        [Required]
        [ForeignKey(nameof(Miasto))]
        public int MiastoId { get; set; }
        public Miasto Miasto { get; set; } = null!;

        // Relacja 1:N - jedna ulica ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();
    }
}