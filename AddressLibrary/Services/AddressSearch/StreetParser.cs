using AddressLibrary.Data;
using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig.Content;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// 🚀 Parser rozbijający string ulicy na komponenty (cecha, tytuł, imiona, nazwiska, pseudonim)
    /// Używa słowników z bazy danych: CechyUlic (przez CechyUlicUtils), TytulyStopnie (przez TitleManager), imiona i nazwiska z TypyUlic
    /// </summary>
    public class StreetParser
    {
        private readonly AddressDbContext _context;

        // Słowniki (cache) - tylko dla danych NIE zarządzanych przez dedykowane managery
        // ❌ USUNIĘTE: private HashSet<string>? _cechy;    // DUPLIKAT CechyUlicUtils.StreetPrefixes
        // ❌ USUNIĘTE: private HashSet<string>? _tytuly;   // DUPLIKAT TitleManager._titlesSet
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

            // CechyUlicUtils i TitleManager są inicjalizowane przez AppCache przed wywołaniem StreetParser.
            // Jeśli jednak nie zostały zainicjalizowane (np. w testach), inicjalizujemy je tutaj.
            if (!CechyUlicUtils.IsInitialized)
            {
                var cacheC = new AddressLibrary.Cache.CechyUlicCache(_context);
                await cacheC.InitializeAsync();
            }

            if (!TitleManager.IsInitialized)
            {
                var cacheT = new AddressLibrary.Cache.TytulyStopnieCache(_context);
                await cacheT.InitializeAsync();
            }

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
          
            Console.WriteLine($"[StreetParser] Załadowano {_imiona.Count} imion, {_nazwiska.Count} nazwisk, {_pseudonimy.Count} pseudonimów");

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

            if (!TitleManager.IsInitialized)
                throw new InvalidOperationException("TitleManager nie został zainicjalizowany.");

            var result = new ParsedStreet();

            // Normalizuj wejściowy string
            var normalized = TextNormalizer.Normalize(streetName);
            // Teraz sztuczka, zastępujemy " z " i " ze " tekstem z podkreśleniami
            // Cel jest taki, żeby do nazwiska poszły teksty z podkreśleniami
            var Przedrostki = new List<string>()
            {
                "de la","del","z","ze","van","de","da","el","von","le","du","a"
            };


            var newText = $" {normalized} ";

            foreach (var przedrostek in Przedrostki.OrderByDescending(x => x.Length))
            {
                var src = $" {przedrostek} ";
                var dst = $" {przedrostek}_";
                newText = newText.Replace(src, dst);
            }

            var words = newText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Przywracamy spację zamiast podkreślenia
            for (int ind = 0; ind < words.Length; ind++)
            {
                words[ind] = words[ind].Replace("_", " ");
            }

            int index = 0;

            // KROK 1: ✅ Wydziel cechę używając CechyUlicUtils zamiast _cechy
            if (index < words.Length && IsCecha(words[index]))
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

            // KROK 3: ✅ Wydziel tytuły używając TitleManager
            var tytuly = new List<string>();
            while (index < words.Length && IsTytul(words[index]))
            {
                tytuly.Add(TitleManager.GetAbbreviation(words[index]));
                index++;
            }
            if (tytuly.Count > 0)
                result.Tytul = string.Join(" ", tytuly);

            // KROK 4: Pozostałe słowa - identyfikuj jako imiona, nazwiska, pseudonimy
            var remainingWords = words.Skip(index).ToList();

            result.Postfiks = "";
            bool czyDopiszDrugieImie = false;
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

                if (czyDopiszDrugieImie && czyImie)
                {
                    // Tutaj kończymy obsługę Jasia i Małgosi
                    result.Imie2 += " " + word;
                    czyDopiszDrugieImie = false;
                    continue;
                }

                if (czyImie && string.IsNullOrEmpty(result.Imie))
                {
                    result.Imie = word;
                    continue;
                }

                if (czyImie && string.IsNullOrEmpty(result.Imie2))
                {
                    // Tutaj obsługujemy Jasia i Małgosi
                    result.Imie2 = word;
                    if (word == "i")
                    {
                        czyDopiszDrugieImie = true;
                    }
                    continue;
                }

                if (czyPseudo && string.IsNullOrEmpty(result.Pseudonim))
                {
                    result.Pseudonim = word;
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

                result.Postfiks += " " + word;
            }

            if (result.Nazwisko == "" && result.Imie2 != "" && _nazwiska!.Contains(result.Imie2))
            {
                // Tutaj trzeba sprawdzić merytorycznie czy nie brakuje nazwiska i czy np. nie zastąpić nazwiska imieniem2
                // Ale trzeba uważać na Mieszka II i podobnych
                var krolewskie = new List<string>() { "I", "II", "III" };
                if (!krolewskie.Contains(result.Imie2))
                {
                    result.Nazwisko = result.Imie2;
                    result.Imie2 = "";
                }
            }

            result.Postfiks = result.Postfiks.Trim();

            return Wyjatki(result);
        }

        public ParsedStreet Wyjatki(ParsedStreet result)
        {
            // Wyjątki!!!
            // księdza biskupa Konstantyna Dominika
            if (result.Prefiks == "" && (result.Tytul.Contains("bp") || result.Tytul.Contains("ks") || result.Tytul == "") && result.Imie == "dominika" && result.Nazwisko == "" && result.Nazwisko2 == "" && result.Pseudonim == "" && result.Postfiks == "")
            {
                result.Tytul = "ks bp";
                result.Imie = "konstantyna";
                result.Nazwisko = "dominika";
                return result;
            }
            // Wyjątki!!!
            // dr Henryka Jordana
            if (result.Prefiks == "" && (result.Tytul.Contains("dr") || result.Tytul == "") && result.Imie == "jordana" && result.Nazwisko == "" && result.Nazwisko2 == "" && result.Pseudonim == "" && result.Postfiks == "")
            {
                result.Tytul = "dr";
                result.Imie = "henryka";
                result.Nazwisko = "jordana";
                return result;
            }
            return result;
        }

        private bool IsPrefiks(string word)
        {
            return word == "im" || word == "imienia";
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza czy słowo jest cechą ulicy używając CechyUlicUtils
        /// </summary>
        private bool IsCecha(string word)
        {
            // Sprawdź we wszystkich wariantach wszystkich cech
            return CechyUlicUtils.StreetPrefixes
                .SelectMany(kv => kv.Value)
                .Any(prefix => prefix.Equals(word, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza czy słowo jest tytułem używając TitleManager
        /// </summary>
        private bool IsTytul(string word)
        {
            // Deleguj sprawdzenie do TitleManager - używa już znormalizowanych danych
            return TitleManager.IsInitialized && !string.IsNullOrEmpty(TitleManager.GetAbbreviation(word));
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