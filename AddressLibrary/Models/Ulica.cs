using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    public class Ulica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        // ✅ ZMIENIONO: Cecha jest teraz kluczem obcym do CechyUlic
        [ForeignKey(nameof(CechaUlicy))]
        public int? CechaUlicyId { get; set; }
        public CechaUlicy? CechaUlicy { get; set; }

        //// ✅ DODANO: Computed property dla zgodności wstecznej
        //[NotMapped]
        //public string? Cecha => CechaUlicy?.Skrot;

        // ✅ ZMIENIONO: Nazwa1 i Nazwa2 są teraz computed properties (nie mapowane do bazy)
        [NotMapped]
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
        public string? Nazwa2
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
        public string Dzielnica { get; set; } = string.Empty;

        // Klucz obcy do miejscowości
        [Required]
        [ForeignKey(nameof(Miasto))]
        public int MiastoId { get; set; }
        public Miasto Miasto { get; set; } = null!;

        // ✅ Klucz obcy do TypUlicy (opcjonalny - nullable)
        [ForeignKey(nameof(TypUlicy))]
        public int? TypUlicyId { get; set; }
        public TypUlicy? TypUlicy { get; set; }

        // Relacja 1:N - jedna ulica ma wiele kodów pocztowych
        public ICollection<KodPocztowy> KodyPocztowe { get; set; } = new List<KodPocztowy>();

        public string Opis { get { return $"{Miasto.Opis} ({Dzielnica}), {CechaUlicy.Opis} {TypUlicy.Opis}".Trim(); } }

    }
}


