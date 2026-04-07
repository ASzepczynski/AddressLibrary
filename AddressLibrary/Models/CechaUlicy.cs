using AddressLibrary.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// S³ownik cech ulic (ul., al., pl., os., itp.)
    /// </summary>
    /// 
    [TableParam(Choice = ChoiceMode.Standard,Description="Cecha ulicy")]
    public class CechaUlicy
    {

        /// <summary>
        /// Pe³na nazwa cechy (np. "ulica", "aleja", "plac")
        /// </summary>
        [Required]
        [MaxLength(50)]
        [MemberParam(Desc = "Nazwa pe³na")]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Skrót cechy (np. "ul.", "al.", "pl.")
        /// </summary>
        [Required]
        [MaxLength(20)]
        [MemberParam(Desc = "Skrót")]
        public string Skrot { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }
        public string Opis() => Skrot;
    }
}