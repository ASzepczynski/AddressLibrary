using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Models
{
    public class Pna
    {
        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty;
        public string Miasto { get; set; } = string.Empty;
        public string Dzielnica { get; set; } = string.Empty;
        public string Ulica { get; set; } = string.Empty;
        public string Gmina { get; set; } = string.Empty;
        public string Powiat { get; set; } = string.Empty;
        public string Wojewodztwo { get; set; } = string.Empty;
        public string Numery { get; set; } = string.Empty;
    }
}
