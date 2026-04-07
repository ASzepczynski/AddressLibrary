using AddressLibrary.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// S³ownik tytu³ów i stopni (gen., p³k., dr., prof., itp.)
    /// </summary>
    [TableParam(Choice = ChoiceMode.Huge, Description = "Tytu³ lub stopieñ")]
    public class TytulStopien
    {
        /// <summary>
        /// Pe³na nazwa tytu³u/stopnia (np. "genera³", "pu³kownik", "doktor")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>
        /// Skrót tytu³u/stopnia (np. "gen.", "p³k.", "dr.")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Skrot { get; set; } = string.Empty;

        /// <summary>
        /// Forma dope³niacza (np. "genera³a", "pu³kownika", "doktora")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Dopelniacz { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }


        public string Opis() => $"{Dopelniacz}";

    }
}