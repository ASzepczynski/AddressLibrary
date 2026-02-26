namespace AddressLibrary.Models
{
    public class TerytTerc
    {
        public int Id { get; set; }
        public string Wojewodztwo { get; set; } = string.Empty;
        public string Powiat { get; set; } = string.Empty;
        public string Gmina { get; set; } = string.Empty;
        public string RodzajGminy { get; set; } = string.Empty;
        public string Nazwa { get; set; } = string.Empty;
        public string NazwaDodatkowa { get; set; } = string.Empty;
        public DateTime StanNa { get; set; } 

    }
}
