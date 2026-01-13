using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Gmina
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(7)]
        public string Kod { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Nazwa { get; set; } = string.Empty;

        // Klucz obcy do powiatu
        [Required]
        [ForeignKey(nameof(Powiat))]
        public int PowiatId { get; set; }
        public Powiat Powiat { get; set; } = null!;

        // Klucz obcy do rodzaju gminy
        [Required]
        [ForeignKey(nameof(RodzajGminy))]
        public int RodzajGminyId { get; set; }
        public RodzajGminy RodzajGminy { get; set; } = null!;

        // Relacja 1:N - jedna gmina ma wiele miejscowoœci
        public ICollection<Miejscowosc> Miejscowosci { get; set; } = new List<Miejscowosc>();
    }
}