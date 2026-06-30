
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [Display(Name = "Teryt SIMC")]
    public class TerytSimc
    {
        [TableVisible(false)]
        public int Id { get; set; }

        [TableVisible(true)]
        [StringLength(100)]
        public string Wojewodztwo { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(100)]
        public string Powiat { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(100)]
        public string Gmina { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(10)]
        public string RodzajGminy { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(50)]
        public string RodzajMiasta { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(10)]
        public string Mz { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Nazwa { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(50)]
        public string Symbol { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(50)]
        public string SymbolPodstawowy { get; set; } = string.Empty;

        public DateTime StanNa { get; set; }
    }
}
