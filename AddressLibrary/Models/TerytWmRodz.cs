
using System.ComponentModel.DataAnnotations;
using DbView;

namespace AddressLibrary.Models
{
    [Display(Name = "Teryt WM Rodz")]
    public class TerytWmRodz
    {
        [TableVisible(false)]
        public int Id { get; set; }

        [TableVisible(true)]
        [StringLength(50)]
        public string RodzajMiasta { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string Nazwa { get; set; } = string.Empty;

        public DateTime StanNa { get; set; }
    }
}
