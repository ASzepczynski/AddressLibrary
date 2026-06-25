using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge)]
    public class TerytTerc
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
        [StringLength(200)]
        public string Nazwa { get; set; } = string.Empty;

        [TableVisible(true)]
        [StringLength(200)]
        public string NazwaDodatkowa { get; set; } = string.Empty;

        public DateTime StanNa { get; set; }

    }
}
