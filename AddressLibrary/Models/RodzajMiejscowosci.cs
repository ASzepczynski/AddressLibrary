using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Standard, Description = "Rodzaj miasta")]
    public class RodzajMiasta
    {
        
        [MemberParam(Desc = "Kod")]
        public string Kod { get; set; } = string.Empty; // Kod z TerytWmRodz
        
        [MemberParam(Desc = "Nazwa")]
        public string Nazwa { get; set; } = string.Empty;

        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        // Relacja 1:N - jeden rodzaj miejscowoœci mo¿e byæ przypisany do wielu miejscowoœci
        public ICollection<Miasto> Miasta { get; set; } = new List<Miasto>();

        public string Opis() => $"{Nazwa} {Kod}";
    }
}