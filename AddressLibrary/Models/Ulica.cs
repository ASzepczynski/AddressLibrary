using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    [TableParam(Choice = ChoiceMode.Huge, Description = "Ulica w mieście")]
    public class Ulica
    {
        [NotMapped]
        [TableVisible(true)]
        [Display(Name = "Miasto")]
        public string NazwaMiasta => Miasto?.Nazwa ?? string.Empty;

        [TableVisible(false)]
        [ForeignKey(nameof(CechaUlicy))]
        public int CechaUlicyId { get; set; }

        [TableVisible(true)]
        [Display(Name = "Cecha ulicy")]
        public CechaUlicy CechaUlicy { get; set; } = null!;

        // ✅ Klucz obcy do TypUlicy (opcjonalny - nullable)
        [TableVisible(false)]
        [ForeignKey(nameof(TypUlicy))]
        public int TypUlicyId { get; set; }

        [TableVisible(true)]
        [Display(Name = "Typ ulicy")]
        public TypUlicy TypUlicy { get; set; } = null!;


        // Nazwa1 i Nazwa2 są teraz computed properties (nie mapowane do bazy)
        [NotMapped]
        [TableVisible(false)]
        [Display(Name = "Nazwa 1")]
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
        [TableVisible(false)]
        [Display(Name = "Nazwa 2")]
        public string Nazwa2
        {
            get
            {
                if (TypUlicy == null)
                    return null;

                var parts = new List<string>();

                // Jeśli jest nazwisko, Nazwa2 = Prefiks + Tytuł + Imię + Imię2 + Nazwisko + (ewentualnie minus) + Nazwisko2 + Pseudonim + Postfiks
                if (!string.IsNullOrWhiteSpace(TypUlicy.Nazwisko))
                {
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Prefiks)) parts.Add(TypUlicy.Prefiks);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Tytul)) parts.Add(TypUlicy.Tytul);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Imie)) parts.Add(TypUlicy.Imie);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Imie2)) parts.Add(TypUlicy.Imie2);
                    if (!string.IsNullOrWhiteSpace(TypUlicy.Nazwisko2))
                    {
                        parts.Add("-");
                        parts.Add(TypUlicy.Nazwisko2);
                    }
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
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Dzielnica")]
        public string Dzielnica { get; set; } = string.Empty;

        // Klucz obcy do miejscowości
        [TableVisible(false)]
        [Required]
        [ForeignKey(nameof(Miasto))]
        public int MiastoId { get; set; }

        [TableVisible(true)]
        [Display(Name = "Miasto")]
        public Miasto Miasto { get; set; } = null!;

        
        [TableVisible(true)]
        [Required]
        [StringLength(10)]
        [Display(Name = "Symbol TERYT")]
        public string Symbol { get; set; } = string.Empty;

        // Pole przechowujące identyfikator ulicy z pliku TERYT` (TerytId)
        [TableVisible(true)]
        [Required]
        [StringLength(100)]
        [Display(Name = "TerytId (nazwa z TERYT)")]
        public string NazwaTeryt { get; set; } = string.Empty;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [TableVisible(false)]
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


