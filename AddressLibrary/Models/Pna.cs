using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Pocztowy numer adresowy")]
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
