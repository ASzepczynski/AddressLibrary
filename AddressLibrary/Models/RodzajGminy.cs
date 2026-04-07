using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Standard, Description = "Rodzaj gminy")]
    public class RodzajGminy
    {
        [Required]
        [MaxLength(1)]
        [MemberParam(Desc = "Kod rodzaju gminy")]
        public string Kod { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [MemberParam(Desc = "Nazwa rodzaju gminy")]
        public string Nazwa { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        // Relacja 1:N - jeden rodzaj gminy mo¿e byæ przypisany do wielu gmin
        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
        
        public string Opis() => Nazwa;
    }
}