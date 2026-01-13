using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Wojewodztwo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2)]
        public string Kod { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nazwa { get; set; } = string.Empty;

        // Relacja 1:N - jedno województwo ma wiele powiatów
        public ICollection<Powiat> Powiaty { get; set; } = new List<Powiat>();
    }
}