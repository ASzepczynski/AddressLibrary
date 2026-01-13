namespace AddressLibrary.Models
{
    public class RodzajGminy
    {
        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty; // "1", "2", "3", "-1"
        public string Nazwa { get; set; } = string.Empty;

        // Relacja 1:N - jeden rodzaj gminy mo¿e byæ przypisany do wielu gmin
        public ICollection<Gmina> Gminy { get; set; } = new List<Gmina>();
    }
}