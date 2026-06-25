using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AddressLibrary.Attributes;
using DbView;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Powiat")]
    public class Powiat
    {

        [Required]
        [MaxLength(100)]
        [MemberParam(Desc = "Nazwa powiatu")]
        public string Nazwa { get; set; } = string.Empty;

        [Required]
        [ForeignKey(nameof(Wojewodztwo))]
        [MemberParam(Desc = "Województwo, do którego nale¿y powiat")]
        public int WojewodztwoId { get; set; }
        public Wojewodztwo Wojewodztwo { get; set; } = null!;

        [Required]
        [MaxLength(4)]
        [MemberParam(Desc = "Kod TERYT powiatu")]
        public string Kod { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
        
        public string Opis() => $"{Nazwa} woj.{Wojewodztwo.Opis()}";
    }
}