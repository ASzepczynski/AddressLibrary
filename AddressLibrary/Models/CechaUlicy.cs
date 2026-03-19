using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// S³ownik cech ulic (ul., al., pl., os., itp.)
    /// </summary>
    public class CechaUlicy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Pe³na nazwa cechy (np. "ulica", "aleja", "plac")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Skrót cechy (np. "ul.", "al.", "pl.")
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Skrot { get; set; } = string.Empty;
    }
}