using AddressLibrary.Models;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// 🚀 Cached wersja Ulica z pre-znormalizowanymi komponentami
    /// Wszystkie komponenty są już znormalizowane (lowercase, bez diakrytyków)
    /// </summary>
    public class UlicaCached
    {
        public int Id { get; set; }
        public int MiastoId { get; set; }
        public string Cecha { get; set; } = string.Empty;
        public Miasto Miasto { get; set; } = null!;
        public string? Dzielnica { get; set; }
        public int? TypUlicyId { get; set; }

        // 🚀 Pre-znormalizowane komponenty z TypUlicy
        public string? Prefiks { get; set; }        // np. "im", "imienia"
        public string? Tytul { get; set; }          // np. "generala", "biskupa", "doktora"
        public string? Imie { get; set; }           // np. "tadeusza"
        public string? Imie2 { get; set; }          // np. "kamila"
        public string? Nazwisko { get; set; }       // np. "ploskiego", "fieldorfa"
        public string? Nazwisko2 { get; set; }      // np. "reymonta"
        public string? Pseudonim { get; set; }      // np. "nila", "zapory"
        public string? Postfiks { get; set; }       // np. "agawy", "sloneczna" (dla ulic nie-osobowych)

        /// <summary>
        /// Zwraca pełną znormalizowaną nazwę (wszystkie komponenty bez cechy)
        /// </summary>
        public string GetFullNormalized()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Prefiks)) parts.Add(Prefiks);
            if (!string.IsNullOrEmpty(Tytul)) parts.Add(Tytul);
            if (!string.IsNullOrEmpty(Imie)) parts.Add(Imie);
            if (!string.IsNullOrEmpty(Imie2)) parts.Add(Imie2);
            if (!string.IsNullOrEmpty(Nazwisko)) parts.Add(Nazwisko);
            if (!string.IsNullOrEmpty(Nazwisko2)) parts.Add(Nazwisko2);
            if (!string.IsNullOrEmpty(Pseudonim)) parts.Add(Pseudonim);
            if (!string.IsNullOrEmpty(Postfiks)) parts.Add(Postfiks);
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Zwraca pełną nazwę z cechą (dla wyświetlania)
        /// </summary>
        public string GetDisplayName()
        {
            var name = GetFullNormalized();
            
            if (string.IsNullOrEmpty(Cecha))
                return name;
            
            return string.IsNullOrEmpty(name) 
                ? Cecha 
                : $"{Cecha} {name}";
        }

        /// <summary>
        /// Sprawdza czy komponenty są puste (ulica nie-osobowa - tylko Postfiks)
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Nazwisko) && 
                   string.IsNullOrEmpty(Imie) && 
                   string.IsNullOrEmpty(Pseudonim) &&
                   string.IsNullOrEmpty(Tytul);
        }
    }
}