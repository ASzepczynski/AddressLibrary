namespace AddressLibrary.Models
{
    public class TerytSimc
    {
        public int Id { get; set; }
        public string Wojewodztwo { get; set; }
        public string Powiat { get; set; }
        public string Gmina { get; set; }
        public string RodzajGminy { get; set; }
        public string RodzajMiasta { get; set; }
        public string Mz { get; set; }
        public string Nazwa { get; set; }
        public string Symbol { get; set; }
        public string SymbolPodstawowy { get; set; }
        public DateTime StanNa { get; set; }
    }
}
