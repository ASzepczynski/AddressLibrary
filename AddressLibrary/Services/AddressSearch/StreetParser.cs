using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig.Content;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// 🚀 Parser rozbijający string ulicy na komponenty (cecha, tytuł, imiona, nazwiska, pseudonim)
    /// Używa słowników z bazy danych: CechyUlic, TytulyStopnie, imiona i nazwiska z TypyUlic
    /// </summary>
    public class StreetParser
    {
        private readonly AddressDbContext _context;

        // Słowniki (cache)
        private HashSet<string>? _cechy;                    // ul, al, pl, os, ...
        private HashSet<string>? _tytuly;                   // gen, bp, plk, dr, prof, ...
        private HashSet<string>? _imiona;                   // tadeusza, krzysztofa, ...
        private HashSet<string>? _nazwiska;                 // ploskiego, fieldorfa, mickiewicza, ...
        private HashSet<string>? _pseudonimy;               // nila, zapory, ...
        private bool _isInitialized;

        public StreetParser(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Inicjalizuje słowniki z bazy danych (wywołaj raz przy starcie)
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            Console.WriteLine($"[StreetParser] === Inicjalizacja słowników ===");

            // 1. Załaduj cechy ulic (ul., al., pl., ...)
            _cechy = (await _context.CechyUlic
                .AsNoTracking()
                .Where(c => c.Id != -1) // ✅ Pomiń sentinel
                .Select(c => c.Skrot)
                .ToListAsync())
                .Select(s => TextNormalizer.Normalize(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine($"[StreetParser] Załadowano {_cechy.Count} cech ulic");

            // 2. Załaduj tytuły (przez TitleManager)
            TitleManager.Initialize(await _context.TytulyStopnie
                .AsNoTracking()
                .Where(t => t.Id != -1) // ✅ Pomiń sentinel
                .ToListAsync());
            
            _tytuly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tytul in await _context.TytulyStopnie
                .AsNoTracking()
                .Where(t => t.Id != -1)
                .ToListAsync())
            {
                if (!string.IsNullOrEmpty(tytul.Dopelniacz))
                    _tytuly.Add(TextNormalizer.Normalize(tytul.Dopelniacz));
                // Dodajemy skróty ale bez kropki
                if (!string.IsNullOrEmpty(tytul.Skrot))
                    _tytuly.Add(TextNormalizer.Normalize(tytul.Skrot));
            }

            Console.WriteLine($"[StreetParser] Załadowano {_tytuly.Count} tytułów");

            // 3. Załaduj imiona, nazwiska, pseudonimy z TypyUlic
            var typyUlic = await _context.TypyUlic
                .AsNoTracking()
                .Where(t => t.Id != -1)
                .ToListAsync();

            _imiona = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _nazwiska = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _pseudonimy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var typ in typyUlic)
            {
                if (!string.IsNullOrEmpty(typ.Imie))
                    _imiona.Add(TextNormalizer.Normalize(typ.Imie));
                if (!string.IsNullOrEmpty(typ.Imie2))
                    _imiona.Add(TextNormalizer.Normalize(typ.Imie2));

                if (!string.IsNullOrEmpty(typ.Nazwisko))
                    _nazwiska.Add(TextNormalizer.Normalize(typ.Nazwisko));
                if (!string.IsNullOrEmpty(typ.Nazwisko2))
                    _nazwiska.Add(TextNormalizer.Normalize(typ.Nazwisko2));

                if (!string.IsNullOrEmpty(typ.Pseudonim))
                    _pseudonimy.Add(TextNormalizer.Normalize(typ.Pseudonim));
            }

            // ✅ WALIDACJA: Sprawdź czy słowniki nie są puste
            if (_nazwiska.Count == 0)
            {
                throw new InvalidOperationException($"StreetParser: Słownik _nazwiska jest PUSTY! TypyUlic ma {typyUlic.Count} rekordów.");
            }

            // ✅ TEST: Czy "chrobrego" jest w słowniku
            var chrobrego = TextNormalizer.Normalize("Chrobrego");
            if (!_nazwiska.Contains(chrobrego))
            {
                // Znajdź rekordy z "Chrobrego" w nazwiskach
                var chrobregoRecords = typyUlic
                    .Where(t => !string.IsNullOrEmpty(t.Nazwisko) && 
                               t.Nazwisko.Contains("Chrobrego", StringComparison.OrdinalIgnoreCase))
                    .Select(t => $"ID={t.Id}, Nazwisko='{t.Nazwisko}'")
                    .ToList();

                throw new InvalidOperationException(
                    $"StreetParser: 'chrobrego' NIE JEST w słowniku _nazwiska!\n" +
                    $"Znaleziono {chrobregoRecords.Count} rekordów z 'Chrobrego': {string.Join(", ", chrobregoRecords)}");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 🚀 Rozbija string ulicy na komponenty
        /// Przykład: "ul. gen. Fieldorfa Nila" -> cecha="ul", tytul="generala", nazwisko="fieldorfa", pseudonim="nila"
        /// </summary>
        public ParsedStreet Parse(string streetName)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("StreetParser nie został zainicjalizowany. Wywołaj InitializeAsync().");

            var result = new ParsedStreet();

            // Normalizuj wejściowy string
            var normalized = TextNormalizer.Normalize(streetName);
            var words = normalized.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            int index = 0;

            // KROK 1: Wydziel cechę (ul, al, pl, os, ...)
            if (index < words.Length && _cechy!.Contains(words[index]))
            {
                result.Cecha = words[index];
                index++;
            }

            // KROK 2: Wydziel prefiks (im, imienia)
            if (index < words.Length && IsPrefiks(words[index]))
            {
                result.Prefiks = words[index];
                index++;
            }

            // KROK 3: Wydziel tytuły (gen., bp, plk, dr, prof, ...)
            var tytuly = new List<string>();
            while (index < words.Length && _tytuly!.Contains(words[index]))
            {
                tytuly.Add(words[index]);
                index++;
            }
            if (tytuly.Count > 0)
                result.Tytul = string.Join(" ", tytuly);

            // KROK 4: Pozostałe słowa - identyfikuj jako imiona, nazwiska, pseudonimy
            var remainingWords = words.Skip(index).ToList();

            result.Postfiks = "";
            foreach (var word in remainingWords)
            {
// Czy to jest Skłodowskiej-Curie?
                var nazwiska = word.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (nazwiska.Length == 2)
                {
                    if (_pseudonimy!.Contains(nazwiska[0]) && _nazwiska!.Contains(nazwiska[1]))
                    {
                        // Grota-Roweckiego
                        result.Pseudonim = nazwiska[0];
                        result.Nazwisko = nazwiska[1];
                        continue;
                    }
                    if (_pseudonimy!.Contains(nazwiska[1]) && _nazwiska!.Contains(nazwiska[0]))
                    {
                        // Roweckiego-Grota
                        result.Pseudonim = nazwiska[1];
                        result.Nazwisko = nazwiska[0];
                        continue;
                    }

                    if (_nazwiska!.Contains(nazwiska[0]) && _nazwiska!.Contains(nazwiska[1]))
                    {
                        // Skłodowskiej-Curie
                        result.Nazwisko = nazwiska[0];
                        result.Nazwisko2 = nazwiska[1];
                        continue;
                    }
                }

                bool czyPseudo = _pseudonimy!.Contains(word);
                bool czyImie = _imiona!.Contains(word);
                bool czyNazwisko = _nazwiska!.Contains(word);
          
                if (czyPseudo && string.IsNullOrEmpty(result.Pseudonim))
                {
                    result.Pseudonim = word;
                    continue;
                }

                if (czyImie && string.IsNullOrEmpty(result.Imie))
                {
                    result.Imie = word;
                    continue;
                }
                if (czyImie && string.IsNullOrEmpty(result.Imie2))
                {
                    result.Imie2 = word;
                    continue;
                }
                if (czyNazwisko && string.IsNullOrEmpty(result.Nazwisko))
                {
                    result.Nazwisko = word;
                    continue;
                }
                if (czyNazwisko && string.IsNullOrEmpty(result.Nazwisko2))
                {
                    result.Nazwisko2 = word;
                    continue;
                }
                
                result.Postfiks += " "+word;
            }
// Tutaj trzeba sprawdzić merytorycznie czy nie brakuje nazwiska i czy np. nie zastąpić nazwiska imieniem2

            result.Postfiks=result.Postfiks.Trim();

             return result;
        }

        private bool IsPrefiks(string word)
        {
            return word == "im" || word == "imienia";
        }
    }

    /// <summary>
    /// Wynik parsowania ulicy
    /// </summary>
    public class ParsedStreet
    {
        public string Cecha { get; set; } = String.Empty;
        public string Prefiks { get; set; } = String.Empty;
        public string Tytul { get; set; } = String.Empty;
        public string Imie { get; set; } = String.Empty;
        public string Imie2 { get; set; } = String.Empty;
        public string Nazwisko { get; set; } = String.Empty;
        public string Nazwisko2 { get; set; } = String.Empty;
        public string Pseudonim { get; set; } = String.Empty;
        public string Postfiks { get; set; } = String.Empty;

        /// <summary>
        /// Zwraca pełną nazwę (bez cechy)
        /// </summary>
        public string GetFullName()
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
    }
}