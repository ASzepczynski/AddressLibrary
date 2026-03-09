// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.
namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Definicja tytułu z skrótem, pełną nazwą i synonimami
    /// </summary>
    public class TitleDefinition
    {
        public string Skrot { get; set; } = string.Empty;
        public string Nazwa { get; set; } = string.Empty;
        public string[] Synonimy { get; set; } = Array.Empty<string>();

        public TitleDefinition(string skrot, string nazwa, params string[] synonimy)
        {
            Skrot = skrot;
            Nazwa = nazwa;
            Synonimy = synonimy;
        }
    }

    /// <summary>
    /// Serwis do zarządzania tytułami (wojskowe, religijne, naukowe)
    /// </summary>
    public static class TitleManager
    {
        // ✅ NOWA STRUKTURA z polskimi znakami: Skrót, Pełna nazwa, Synonimy
        private static readonly TitleDefinition[] titles_pl = new[]
        {
            // ===== WOJSKOWE =====
            new TitleDefinition("płk.", "pułkownika", "płk", "plk", "pulkownika"),
            new TitleDefinition("mjr.", "majora", "mjr"),
            new TitleDefinition("kpt.", "kapitana", "kpt"),
            new TitleDefinition("por.", "porucznika", "por"),
            new TitleDefinition("ppor.", "podporucznika", "ppor"),
            new TitleDefinition("gen.", "generała", "gen", "generala"),
            new TitleDefinition("ppłk.", "podpułkownika", "ppłk", "pplk", "podpulkownika"),
            new TitleDefinition("rtm.", "rotmistrza", "rtm", "rotm"),
            new TitleDefinition("sierż.", "sierżanta", "sierż", "sierz", "sierzanta"),
            new TitleDefinition("marsz.", "marszałka", "marsz", "marszalka"),
            new TitleDefinition("adm.", "admirała", "adm", "admirala"),
            new TitleDefinition("adw.", "adwokata", "adw"),
            new TitleDefinition("kmdr.", "komandora", "kmdr"),
            new TitleDefinition("bryg.", "brygadiera", "bryg"),
            new TitleDefinition("hetm.", "hetmana","hetm"),
            new TitleDefinition("kpr.", "kaprala", "kpr"),
            new TitleDefinition("kap.", "kapelana", "kap"),
            new TitleDefinition("dh.", "druha", "dh"),
            new TitleDefinition("hcm.", "harcmistrza", "hcm","harcm.","harcm"),

            // ===== RELIGIJNE =====
            new TitleDefinition("prym. kard.", "prymasa kardynała", "prym kard"),
            new TitleDefinition("prym.", "prymasa", "prym"),
            new TitleDefinition("św.", "świętego", "św", "sw", "swietego"),
            new TitleDefinition("św.", "świętej", "św", "sw", "swietej"),
            new TitleDefinition("ks.", "księdza", "ks", "ksiedza"),
            new TitleDefinition("bp.", "biskupa", "bp", "bpa"),
            new TitleDefinition("abp.", "arcybiskupa", "abp", "abpa"),
            new TitleDefinition("kard.", "kardynała", "kard", "kardynala"),
            new TitleDefinition("kan.", "kanonika", "kan"),
            new TitleDefinition("bł.", "błogosławionego", "bł", "bl", "blogoslawionego","błog.","błog","blog.","blog"),
            new TitleDefinition("bł.", "błogosławionej", "bł", "bl", "blogoslawionej","błog.","błog","blog.","blog"),
            new TitleDefinition("br.", "brata", "br"),
            new TitleDefinition("br.", "braci", "br"),
            new TitleDefinition("o.", "ojca", "o"),
            new TitleDefinition("s.", "siostry", "s"),

            // ===== NAUKOWE =====
            new TitleDefinition("prof. hab. n. med.", "profesora habilitowanego nauk medycznych"),
            new TitleDefinition("dr.", "doktora", "dr","dra"),
            new TitleDefinition("prof.", "profesora", "prof"),
            new TitleDefinition("inż.", "inżyniera", "inż", "inz", "inzyniera"),
            new TitleDefinition("mgr.", "magistra", "mgr"),
            new TitleDefinition("lek.", "lekarza", "lek"),
            new TitleDefinition("lek. med.", "lekarza medycyny", "lek med"),

            // ===== SZLACHECKIE =====
            new TitleDefinition("księcia", "księcia","ks","ksiecia"),
            new TitleDefinition("króla", "króla", "kr.","kr","krola"),
            new TitleDefinition("królowej", "królowej", "kr.","kr","krolowej"),
            new TitleDefinition("kanc.", "kanclerza","kanc"),

            // ===== INNE =====
            new TitleDefinition("rodz.", "rodziny", "rodz"),
            new TitleDefinition("burm.", "burmistrza", "burm."),
            new TitleDefinition("wójta", "wójta", "wojta")
        };

        // ✅ MAPA: znormalizowana forma → TitleDefinition (lazy initialization)
        private static Dictionary<string, TitleDefinition>? _titleMap;
        private static Dictionary<string, TitleDefinition> titleMap
        {
            get
            {
                if (_titleMap == null)
                {
                    _titleMap = new Dictionary<string, TitleDefinition>(StringComparer.OrdinalIgnoreCase);

                    foreach (var title in titles_pl)
                    {
                        // Dodaj skrót (bez kropki i bez polskich znaków)
                        var normalizedSkrot = UliceUtils.RemoveDiacritics(title.Skrot.Replace(".", ""));
                        if (!_titleMap.ContainsKey(normalizedSkrot))
                            _titleMap[normalizedSkrot] = title;

                        // Dodaj pełną nazwę (bez polskich znaków)
                        var normalizedNazwa = UliceUtils.RemoveDiacritics(title.Nazwa);
                        if (!_titleMap.ContainsKey(normalizedNazwa))
                            _titleMap[normalizedNazwa] = title;

                        // Dodaj synonimy (bez polskich znaków)
                        foreach (var synonim in title.Synonimy)
                        {
                            var normalizedSynonim = UliceUtils.RemoveDiacritics(synonim);
                            if (!_titleMap.ContainsKey(normalizedSynonim))
                                _titleMap[normalizedSynonim] = title;
                        }
                    }
                }
                return _titleMap;
            }
        }

        // ✅ HASHSET dla szybkiego sprawdzania czy słowo to tytuł (lazy initialization)
        private static HashSet<string>? _titlesSet;
        private static HashSet<string> titlesSet
        {
            get
            {
                if (_titlesSet == null)
                {
                    _titlesSet = new HashSet<string>(titleMap.Keys, StringComparer.OrdinalIgnoreCase);
                }
                return _titlesSet;
            }
        }

        /// <summary>
        /// Usuwa tytuły wojskowe, religijne, naukowe z tekstu (case-insensitive, bez polskich znaków)
        /// </summary>
        public static string RemoveTitles(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // ✅ POPRAWKA: Normalizuj każde słowo przed porównaniem (usuń polskie znaki + lowercase)
            var filtered = words.Where(w =>
            {
                var normalized = UliceUtils.RemoveDiacritics(w.Replace(".", "").ToLowerInvariant());
                return !titlesSet.Contains(normalized);
            }).ToList();

            return string.Join(" ", filtered);
        }

        /// <summary>
        /// Wyodrębnia tytuły z nazwy ulicy
        /// Przykład: "prof. dr mgr inż. Andrzej Szepczyński" → ("prof. dr mgr inż.", "Andrzej Szepczyński")
        /// </summary>
        /// <param name="streetName">Pełna nazwa ulicy z tytułami</param>
        /// <returns>Tuple (tytuły, nazwa bez tytułów)</returns>
        public static (string titles, string nameWithoutTitles) SplitInitialTitle(string streetName)
        {
            if (string.IsNullOrWhiteSpace(streetName))
                return (string.Empty, streetName ?? string.Empty);

            var words = streetName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var extractedTitles = new List<string>();
            var currentIndex = 0;

            // Iteruj po słowach i zbieraj tytuły z początku
            while (currentIndex < words.Length)
            {
                var word = words[currentIndex];

                // Normalizuj słowo (usuń kropkę, polskie znaki, lowercase)
                var normalizedWord = UliceUtils.RemoveDiacritics(word.Replace(".", "").ToLowerInvariant());

                // Sprawdź czy to tytuł
                if (titlesSet.Contains(normalizedWord))
                {
                    // Dodaj oryginalne słowo (z kropką jeśli była)
                    extractedTitles.Add(word);
                    currentIndex++;
                }
                else
                {
                    // Napotkano słowo, które nie jest tytułem - przerywamy
                    break;
                }
            }

            // Złóż wynik
            var titlesString = string.Join(" ", extractedTitles);
            var remainingWords = words.Skip(currentIndex).ToArray();
            var nameWithoutTitles = string.Join(" ", remainingWords);

            return (titlesString, nameWithoutTitles);
        }

        /// <summary>
        /// Normalizuje tytuły - zamienia pełne formy i synonimy na skróty
        /// Przykład: "doktora profesora" → "dr. prof."
        /// </summary>
        /// <param name="titles">Ciąg tytułów do znormalizowania</param>
        /// <returns>Znormalizowany ciąg tytułów ze skrótami</returns>
        public static string NormalizeTitles(string titles)
        {
            if (string.IsNullOrWhiteSpace(titles))
                return string.Empty;

            var words = titles.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedTitles = new List<string>();

            foreach (var word in words)
            {
                // Normalizuj słowo (usuń kropkę, polskie znaki, lowercase)
                var normalizedWord = UliceUtils.RemoveDiacritics(word.Replace(".", "").ToLowerInvariant());

                // ✅ ZMIANA: Szukaj w titles_pl zamiast titleMap
                TitleDefinition? foundTitle = null;

                foreach (var titleDef in titles_pl)
                {
                    // Sprawdź skrót (bez kropki i polskich znaków)
                    var normalizedSkrot = UliceUtils.RemoveDiacritics(titleDef.Skrot.Replace(".", "").ToLowerInvariant());
                    if (normalizedWord == normalizedSkrot)
                    {
                        foundTitle = titleDef;
                        break;
                    }

                    // Sprawdź pełną nazwę (bez polskich znaków)
                    var normalizedNazwa = UliceUtils.RemoveDiacritics(titleDef.Nazwa.ToLowerInvariant());
                    if (normalizedWord == normalizedNazwa)
                    {
                        foundTitle = titleDef;
                        break;
                    }

                    // Sprawdź synonimy (bez polskich znaków)
                    foreach (var synonim in titleDef.Synonimy)
                    {
                        var normalizedSynonim = UliceUtils.RemoveDiacritics(synonim.ToLowerInvariant());
                        if (normalizedWord == normalizedSynonim)
                        {
                            foundTitle = titleDef;
                            break;
                        }
                    }

                    if (foundTitle != null)
                        break;
                }

                // Dodaj skrót lub oryginalne słowo
                if (foundTitle != null)
                {
                    // Dodaj skrót (jeśli jeszcze go nie ma w liście)
                    if (!normalizedTitles.Contains(foundTitle.Skrot))
                    {
                        normalizedTitles.Add(foundTitle.Skrot);
                    }
                }
                else
                {
                    // Jeśli nie znaleziono, zachowaj oryginalne słowo
                    normalizedTitles.Add(word);
                }
            }

            return string.Join(" ", normalizedTitles);
        }

        /// <summary>
        /// Pobiera pełną nazwę tytułu na podstawie skrótu lub synonimu
        /// Przykład: "płk" → "pułkownika", "gen." → "generała"
        /// </summary>
        public static string? GetFullName(string titleOrAbbreviation)
        {
            if (string.IsNullOrWhiteSpace(titleOrAbbreviation))
                return null;

            var normalized = UliceUtils.RemoveDiacritics(titleOrAbbreviation.Replace(".", "").ToLowerInvariant());

            if (titleMap.TryGetValue(normalized, out var titleDef))
            {
                return titleDef.Nazwa;
            }

            return null;
        }

        /// <summary>
        /// Pobiera skrót tytułu na podstawie pełnej nazwy lub synonimu
        /// Przykład: "pułkownika" → "płk.", "generała" → "gen."
        /// </summary>
        public static string? GetAbbreviation(string titleOrFullName)
        {
            if (string.IsNullOrWhiteSpace(titleOrFullName))
                return null;

            var normalized = UliceUtils.RemoveDiacritics(titleOrFullName.Replace(".", "").ToLowerInvariant());

            if (titleMap.TryGetValue(normalized, out var titleDef))
            {
                return titleDef.Skrot;
            }

            return null;
        }
    }
}
