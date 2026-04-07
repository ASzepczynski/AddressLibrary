using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge)]
    public class TerytSimc
    {
        public int Id { get; set; }
        public string Wojewodztwo { get; set; } = string.Empty;
        public string Powiat { get; set; } = string.Empty;
        public string Gmina { get; set; } = string.Empty;
        public string RodzajGminy { get; set; } = string.Empty;
        public string RodzajMiasta { get; set; } = string.Empty;
        public string Mz { get; set; } = string.Empty;
        public string Nazwa { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string SymbolPodstawowy { get; set; } = string.Empty;
        public DateTime StanNa { get; set; }
    }
}
