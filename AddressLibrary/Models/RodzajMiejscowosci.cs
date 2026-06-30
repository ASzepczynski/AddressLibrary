
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AddressLibrary.Models
{
    [Display(Name = "Rodzaj miasta")]
	public class RodzajMiasta
    {
        
        [Display(Name = "Kod")]
        public string Kod { get; set; } = string.Empty; // Kod z TerytWmRodz
        
        [Display(Name = "Nazwa")]
        public string Nazwa { get; set; } = string.Empty;

        [Display(Name = "ID")]
        public int Id { get; set; }

		// Relacja 1:N - jeden rodzaj miejscowoœci mo¿e byæ przypisany do wielu miejscowoœci
		[TableVisible(false)]
		public ICollection<Miasto> Miasta { get; set; } = new List<Miasto>();

        public string Opis() => $"{Nazwa} {Kod}";
    }
}