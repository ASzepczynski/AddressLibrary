using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddressLibrary.Models;

namespace AddressLibrary.Structures
{
    public class ResultList
    {
        public TerytUlic? Ulica { get; set; }
        public string? WojewodztwoNazwa { get; set; }
        public string? PowiatNazwa { get; set; }
        public string? GminaNazwa { get; set; }
        public TerytSimc? Miasto { get; set; }
       
    }
}
