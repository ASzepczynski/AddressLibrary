using AddressLibrary.Attributes;
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
        public string Prefiks { get; set; } = string.Empty;

        /// <summary>
        /// Klucz obcy do tabeli TytulyStopnie (np. "dr.", "prof.", "p³k.")
        /// Wartoœæ -1 oznacza brak tytu³u
        /// </summary>
        [ForeignKey(nameof(TytulStopien))]
        public int TytulStopienId { get; set; } = -1;

        /// <summary>
        /// Relacja do tabeli TytulyStopnie
        /// </summary>
        public TytulStopien? TytulStopien { get; set; }

        /// <summary>
        /// Computed property zwracaj¹ce skrót tytu³u dla zachowania kompatybilnoœci wstecznej
        /// </summary>
        [NotMapped]
        public string? Tytul => TytulStopien?.Skrot == null ? string.Empty : TytulStopien.Skrot;

        /// <summary>
        /// Pierwsze imiê (np. "Stanis³awa")
        /// </summary>
        public string Imie { get; set; } = string.Empty;

        /// <summary>
        /// Drugie imiê (np. "Kamila" w "Krzysztofa Kamila Baczyñskiego")
        /// </summary>
        public string Imie2 { get; set; } = string.Empty;

        /// <summary>
        /// Pierwsze nazwisko (np. "Mickiewicza")
        /// </summary>
        public string Nazwisko { get; set; } = string.Empty;

        /// <summary>
        /// Drugie nazwisko (np. "Reymonta" w "W³adys³awa Stanis³awa Reymonta")
        /// </summary>
        public string Nazwisko2 { get; set; } = string.Empty;

        /// <summary>
        /// Pseudonim (np. "Zapory", "Zoœki", "Nila")
        /// </summary>
        public string Pseudonim { get; set; } = string.Empty;

        /// <summary>
        /// Postfiks/przydomek (np. dodatkowe informacje po pseudonimie)
        /// </summary>
        public string Postfiks { get; set; } = string.Empty;

        /// <summary>
        /// Identyfikator (klucz g³ówny)
        /// </summary>
        public int Id { get; set; }

        public string Opis() => $"{Prefiks} {TytulStopien?.Dopelniacz} {Imie} {Imie2} {Nazwisko} {Nazwisko2} {Pseudonim} {Postfiks}".Trim();
    }
}