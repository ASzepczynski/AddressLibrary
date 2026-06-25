using AddressLibrary.Attributes;
using DbView;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddressLibrary.Models
{
    /// <summary>
    /// Model reprezentuj¹cy typy ulic osobowych z pe³n¹ dekompozycj¹ nazwy
    /// </summary>
    [TableParam(Choice = ChoiceMode.Huge, Description = "Typ ulicy")]
    public class TypUlicy
    {
        /// <summary>
        /// Prefiks (np. "p³k.", "gen.", "ks.", "im.", "imienia")
        /// </summary>
        [TableVisible(true)]
        [StringLength(50)]
        [Display(Name = "Prefiks")]
        public string Prefiks { get; set; } = string.Empty;

        /// <summary>
        /// Klucz obcy do tabeli TytulyStopnie (np. "dr.", "prof.", "p³k.")
        /// Wartoœæ -1 oznacza brak tytu³u
        /// </summary>
        [TableVisible(false)]
        [ForeignKey(nameof(TytulStopien))]
        public int TytulStopienId { get; set; } = -1;

        /// <summary>
        /// Relacja do tabeli TytulyStopnie
        /// </summary>
        [TableVisible(true)]
        [Display(Name = "Tytu³/stopieñ")]
        public TytulStopien? TytulStopien { get; set; }

        /// <summary>
        /// Computed property zwracaj¹ce skrót tytu³u dla zachowania kompatybilnoœci wstecznej
        /// </summary>
        [NotMapped]
        [TableVisible(false)]
        public string? Tytul => TytulStopien?.Skrot == null ? string.Empty : TytulStopien.Skrot;

        /// <summary>
        /// Pierwsze imiê (np. "Stanis³awa")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Imiê")]
        public string Imie { get; set; } = string.Empty;

        /// <summary>
        /// Drugie imiê (np. "Kamila" w "Krzysztofa Kamila Baczyñskiego")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Imiê2")]
        public string Imie2 { get; set; } = string.Empty;

        /// <summary>
        /// Pierwsze nazwisko (np. "Mickiewicza")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        /// <summary>
        /// Drugie nazwisko (np. "Reymonta" w "W³adys³awa Stanis³awa Reymonta")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Nazwisko2")]
        public string Nazwisko2 { get; set; } = string.Empty;

        /// <summary>
        /// Pseudonim (np. "Zapory", "Zoœki", "Nila")
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Pseudonim")]
        public string Pseudonim { get; set; } = string.Empty;

        /// <summary>
        /// Postfiks/przydomek (np. dodatkowe informacje po pseudonimie)
        /// </summary>
        [TableVisible(true)]
        [StringLength(200)]
        [Display(Name = "Postfiks")]
        public string Postfiks { get; set; } = string.Empty;

        /// <summary>
        /// Identyfikator (klucz g³ówny)
        /// </summary>
        [TableVisible(false)]
        public int Id { get; set; }

        public string Opis()
        {
            // Buduj listê czêœci, pomijaj¹c puste wartoœci
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Prefiks)) parts.Add(Prefiks);
            if (!string.IsNullOrWhiteSpace(Tytul)) parts.Add(Tytul);
            if (!string.IsNullOrWhiteSpace(Imie)) parts.Add(Imie);
            if (!string.IsNullOrWhiteSpace(Imie2)) parts.Add(Imie2);

            // Jeœli istnieje Nazwisko2, po³¹cz je ³¹cznikiem z Nazwisko
            string lastName = string.Empty;
            if (!string.IsNullOrWhiteSpace(Nazwisko))
            {
                if (!string.IsNullOrWhiteSpace(Nazwisko2))
                    lastName = Nazwisko + "-" + Nazwisko2;
                else
                    lastName = Nazwisko;
            }
            else if (!string.IsNullOrWhiteSpace(Nazwisko2))
            {
                lastName = Nazwisko2;
            }

            if (!string.IsNullOrWhiteSpace(lastName)) parts.Add(lastName);
            if (!string.IsNullOrWhiteSpace(Pseudonim)) parts.Add(Pseudonim);
            if (!string.IsNullOrWhiteSpace(Postfiks)) parts.Add(Postfiks);

            var result = string.Join(" ", parts);

            // Usuñ nadmiarowe spacje (wiele spacji -> jedna) i przytnij
            result = System.Text.RegularExpressions.Regex.Replace(result, "\\s+", " ").Trim();
            return result;
        }
    }
}