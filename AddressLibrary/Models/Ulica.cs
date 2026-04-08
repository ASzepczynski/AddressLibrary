using AddressLibrary.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Ulica w mieście")]
    public class Ulica
    {
        [NotMapped]
        [MemberParam(Desc = "Miasto")]
        public string NazwaMiasta => Miasto?.Nazwa ?? string.Empty;

        [ForeignKey(nameof(CechaUlicy))]
        [MemberParam(Desc = "Cecha ulicy")]
        public int CechaUlicyId { get; set; }
        public CechaUlicy CechaUlicy { get; set; } = null!;

        // ✅ Klucz obcy do TypUlicy (opcjonalny - nullable)
        [ForeignKey(nameof(TypUlicy))]
        [MemberParam(Desc = "Typ ulicy")]
        public int TypUlicyId { get; set; }
        public TypUlicy TypUlicy { get; set; } = null!;


        // Nazwa1 i Nazwa2 są teraz computed properties (nie mapowane do bazy)
        [NotMapped]
        [MemberParam(Desc = "Nazwa 1", Visible = false)]
        public string Nazwa1
        {
            get
            {
                if (TypUlicy == null)
                    return string.Empty;

                // Jeśli jest nazwisko, Nazwa1 = nazwisko
                if (!string.IsNullOrWhiteSpace(TypUlicy.Nazwisko))
                    return TypUlicy.Nazwisko;

                // Jeśli nie ma nazwiska, ale jest imię, Nazwa1 = imię
                if (!string.IsNullOrWhiteSpace(TypUlicy.Imie))
                    return TypUlicy.Imie;

                // W przeciwnym razie Nazwa1 = Prefiks + Tytuł + Pseudonim + Postfiks
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(TypUlicy.Prefiks)) parts.Add(TypUlicy.Prefiks);
                if (!string.IsNullOrWhiteSpace(TypUlicy.Tytul)) parts.Add(TypUlicy.Tytul);
                if (!string.IsNullOrWhiteSpace(TypUlicy.Pseudonim)) parts.Add(TypUlicy.Pseudonim);
                if (!string.IsNullOrWhiteSpace(TypUlicy.Postfiks)) parts.Add(TypUlicy.Postfiks);

                return string.Join(" ", parts);
            }
        }

        [NotMapped]
        [MemberParam(Desc = "Nazwa 2", Visible = false)]
        public string Nazwa2
        {
            get
            {
                if (TypUlicy == null)
                    return null;

                var parts = new List<string>();

                // Jeśli jest nazwisko, Nazwa2 = Prefiks + Tytuł + Imię + Imię2 + Nazwisko2 + Pseudonim + Postfiks
                if (!string.IsNullOrWhiteSpace(TypUlicy.Nazwisko))
                {
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Prefiks)) parts.Add(TypUlicy.Prefiks);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Tytul)) parts.Add(TypUlicy.Tytul);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Imie)) parts.Add(TypUlicy.Imie);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Imie2)) parts.Add(TypUlicy.Imie2);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Nazwisko2)) parts.Add(TypUlicy.Nazwisko2);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Pseudonim)) parts.Add(TypUlicy.Pseudonim);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Postfiks)) parts.Add(TypUlicy.Postfiks);
                }
                // Jeśli nie ma nazwiska, ale jest imię, Nazwa2 = Prefiks + Tytuł + Imię2 + Pseudonim + Postfiks
                else if (!string.IsNullOrWhiteSpace(TypUlicy.Imie))
                {
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Prefiks)) parts.Add(TypUlicy.Prefiks);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Tytul)) parts.Add(TypUlicy.Tytul);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Imie2)) parts.Add(TypUlicy.Imie2);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Pseudonim)) parts.Add(TypUlicy.Pseudonim);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Postfiks)) parts.Add(TypUlicy.Postfiks);
                }
                // W przeciwnym razie Nazwa2 jest pusta
                else
                {
                    return null;
                }

                return parts.Count > 0 ? string.Join(" ", parts) : null;
            }
        }

        // ✅ DODANO: Pole dzielnica
        [MaxLength(200)]
        [MemberParam(Desc = "Dzielnica")]
        public string Dzielnica { get; set; } = string.Empty;

        // Klucz obcy do miejscowości
        [Required]
        [ForeignKey(nameof(Miasto))]
        [MemberParam(Desc = "Miasto")]
        public int MiastoId { get; set; }
        public Miasto Miasto { get; set; } = null!;

        
        [Required]
        [MaxLength(10)]
        [MemberParam(Desc = "Symbol TERYT")]
        public string Symbol { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MemberParam(Desc = "ID")]
        public int Id { get; set; }

        // Relacja 1:N - jedna ulica ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();

        public string Opis()
        {
            var dzielnicaPart = !string.IsNullOrWhiteSpace(Dzielnica) ? $" ({Dzielnica})" : "";
            return $"{Miasto.Opis()}{dzielnicaPart}, {CechaUlicy.Opis()} {TypUlicy.Opis()}".Trim();
        }
    }
}


