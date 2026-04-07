using AddressLibrary.Attributes;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge)]
    public class TerytWmRodz
    {
        public int Id { get; set; }
        public string RodzajMiasta { get; set; } = string.Empty;
        public string Nazwa { get; set; } = string.Empty;
        public DateTime StanNa { get; set; }
    }
}
