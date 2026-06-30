using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// S³ownik cech ulic (ul., al., pl., os., itp.)
    /// </summary>
    /// 
    [Display(Name = "Rodzaje firm")]
    public class CechaUlicy
    {

        /// <summary>
        /// Pe³na nazwa cechy (np. "ulica", "aleja", "plac")
        /// </summary>
        [TableVisible(true)]
        [Required]
        [StringLength(50)]
        [Display(Name = "Nazwa pe³na")]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Skrót cechy (np. "ul.", "al.", "pl.")
        /// </summary>
        [TableVisible(true)]
        [Required]
        [StringLength(20)]
        [Display(Name = "Skrót")]
        public string Skrot { get; set; } = string.Empty;

        [TableVisible(false)]
        public int Id { get; set; }
        public string Opis() => Skrot;
    }
}