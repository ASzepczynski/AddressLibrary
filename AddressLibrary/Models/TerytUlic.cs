namespace AddressLibrary.Models
{
    public class TerytUlic
    {
        public int Id { get; set; }
        public string Wojewodztwo { get; set; } = string.Empty;
        public string Powiat { get; set; } = string.Empty;
        public string Gmina { get; set; } = string.Empty;
        public string RodzajGminy { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string SymbolUlicy { get; set; } = string.Empty;
        public string Cecha { get; set; } = string.Empty;
        public string Nazwa1 { get; set; } = string.Empty;
        public string Nazwa2 { get; set; } = string.Empty;
        public DateTime StanNa { get; set; }
    }
}
