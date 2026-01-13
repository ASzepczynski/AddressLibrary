namespace AddressLibrary.Models
{
    public class RodzajMiejscowosci
    {
        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty; // Kod z TerytWmRodz
        public string Nazwa { get; set; } = string.Empty;

        // Relacja 1:N - jeden rodzaj miejscowoœci mo¿e byæ przypisany do wielu miejscowoœci
        public ICollection<Miejscowosc> Miejscowosci { get; set; } = new List<Miejscowosc>();
    }
}