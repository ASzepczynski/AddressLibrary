using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [Display(Name = "Pocztowy numer adresowy")]
    public class Pna
    {
        [TableVisible(false)]
        public int Id { get; set; }

        [TableVisible(true)]
        [StringLength(20)]
        public string Kod { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Miasto { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Dzielnica { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Ulica { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Gmina { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Powiat { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(100)]
        public string Wojewodztwo { get; set; } = string.Empty;

        [TableVisible(true)]
        public string Numery { get; set; } = string.Empty;
    }
}
