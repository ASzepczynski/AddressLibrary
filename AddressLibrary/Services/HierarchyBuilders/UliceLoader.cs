using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Utils;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch;
using AddressLibrary.Structures;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class UliceLoader : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly HierarchyStreetLogger _logger;
        private readonly PrefixChangeLogger _prefixLogger;
        private readonly StreetNamePersonalConverter _personalConverter;

        public UliceLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new HierarchyStreetLogger(appDataPath);
            _prefixLogger = new PrefixChangeLogger(appDataPath);
            _personalConverter = new StreetNamePersonalConverter(appDataPath ?? string.Empty);

            _logger.LogInfo($"Załadowano {_personalConverter.Count} konwersji ulic osobowych z Excel");

            // Debug słownika konwersji
            _logger.LogInfo("=== ZAWARTOŚĆ SŁOWNIKA KONWERSJI ===");
            var debugKeys = _personalConverter.GetAllKeys();
            foreach (var key in debugKeys.Take(20))
            {
                _logger.LogInfo($"  Klucz: ('{key.Item1}', '{key.Item2}')");
            }
        }

        private NameCorrectionHelper _corrections;
        public async Task LoadAsync(
            List<TerytUlic> ulicData,
            Dictionary<string, Miasto> miastoDict,
            string? appDataPath)
        {
            _logger.LogInfo($"Liczba ulic do przetworzenia: {ulicData.Count}");
            _logger.LogInfo($"Liczba miejscowości w słowniku: {miastoDict.Count}");

            _corrections = new NameCorrectionHelper(appDataPath);
            Console.WriteLine($"Załadowano {_corrections.Count} korekt ({_corrections.GetCountByType("M")} miast, {_corrections.GetCountByType("U")} ulic)");

            // ✅ DODANO: Wczytaj słownik TerytUlicPoprawki
            _logger.LogInfo("Wczytywanie słownika TerytUlicPoprawki...");
            var terytUlicPoprawkiDict = TerytUlicPoprawkiDictionary.Load(appDataPath, _logger);
            _logger.LogInfo($"Załadowano {terytUlicPoprawkiDict.Count} wpisów ze słownika TerytUlicPoprawki");

            // ✅ DODANO: Załaduj mapowanie TypyUlic z bazy (TypUlicy -> Id)
            _logger.LogInfo("Wczytywanie mapowania TypyUlic z bazy danych...");
            var typyUlicDict = await _context.TypyUlic
                .AsNoTracking()
                .ToDictionaryAsync(
                    t => new TypUlicyKey
                    {
                        Prefiks = t.Prefiks ?? "",
                        Tytul = t.Tytul ?? "",
                        Imie = t.Imie ?? "",
                        Imie2 = t.Imie2 ?? "",
                        Nazwisko = t.Nazwisko ?? "",
                        Nazwisko2 = t.Nazwisko2 ?? "",
                        Pseudonim = t.Pseudonim ?? "",
                        Postfiks = t.Postfiks ?? ""
                    },
                    t => t.Id,
                    new TypUlicyKeyEqualityComparer()
                );
            _logger.LogInfo($"Załadowano {typyUlicDict.Count} wpisów z tabeli TypyUlic");

            int przetworzono = 0;
            int brakujacych = 0;
            int cityWithRightsProcessed = 0;
            int regularProcessed = 0;
            int convertedFromExcel = 0;
            int prefixChanges = 0;
            int typUlicyAssigned = 0; // ✅ DODANO: Licznik przypisanych typów ulic

            // Dla miast na prawach powiatu - załaduj raz na początku
            _logger.LogInfo("Przygotowuję mapowanie miast na prawach powiatu...");
            var miastaNaPrawachPowiatuDict = new Dictionary<string, Miasto>();

            // Załaduj wszystkie gminy z powiatami
            var gminyAll = await _context.Gminy
                .Include(g => g.Powiat)
                    .ThenInclude(p => p.Wojewodztwo)
                .ToListAsync();

            _logger.LogInfo($"Załadowano {gminyAll.Count} gmin z bazy");

            // POPRAWKA: Filtruj gminy w miastach na prawach powiatu
            var gminyWMiastachNaPrawachPowiatu = gminyAll
                .Where(g => g.Powiat.Kod.EndsWith("61") || g.Powiat.Kod.EndsWith("62") ||
                           g.Powiat.Kod.EndsWith("63") || g.Powiat.Kod.EndsWith("64") ||
                           g.Powiat.Kod.EndsWith("65"))
                .ToList();

            _logger.LogInfo($"Znaleziono {gminyWMiastachNaPrawachPowiatu.Count} gmin w miastach na prawach powiatu");

            foreach (var gmina in gminyWMiastachNaPrawachPowiatu)
            {
                var kodPowiatu = gmina.Powiat.Kod;
                var miasto = miastoDict.Values.FirstOrDefault(m => m.GminaId == gmina.Id);

                if (miasto != null)
                {
                    if (!miastaNaPrawachPowiatuDict.ContainsKey(kodPowiatu))
                    {
                        miastaNaPrawachPowiatuDict[kodPowiatu] = miasto;
                        _logger.LogInfo($"Zarejestrowano miasto na prawach powiatu: {miasto.Nazwa} (MiastoId={miasto.Id}), Gmina: {gmina.Nazwa} (GminaId={gmina.Id}), Powiat: {kodPowiatu}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Nie znaleziono miasta dla gminy {gmina.Nazwa} (GminaId={gmina.Id})");
                }
            }

            _logger.LogInfo($"Mapowanie miast na prawach powiatu zawiera {miastaNaPrawachPowiatuDict.Count} wpisów");

            // Wyświetl wszystkie wpisy
            foreach (var kvp in miastaNaPrawachPowiatuDict)
            {
                _logger.LogInfo($"  [{kvp.Key}] => {kvp.Value.Nazwa} (MiastoId={kvp.Value.Id})");
            }

            _logger.LogInfo("Przetwarzam ulice...");

            // Lista wszystkich ulic do wstawienia
            var allUlice = new List<Ulica>(ulicData.Count);

            // Główna pętla - przygotowanie danych
            var wojDict = _context.Wojewodztwa.AsNoTracking().ToDictionary(x => x.Kod);
            var powDict = _context.Powiaty.AsNoTracking().ToDictionary(x => x.Kod);
            var gmiDict = _context.TerytTerc.AsNoTracking().ToDictionary(x => (x.Wojewodztwo, x.Powiat, x.Gmina, x.RodzajGminy));
            var miaDict = _context.TerytSimc.AsNoTracking().ToDictionary(x => x.Symbol);

            var resultList = ulicData.Select(u => new ResultList
            {
                Ulica = u,
                WojewodztwoNazwa = wojDict.GetValueOrDefault(u.Wojewodztwo)?.Nazwa,
                PowiatNazwa = powDict.GetValueOrDefault(u.Wojewodztwo + u.Powiat)?.Nazwa,
                GminaNazwa = gmiDict.GetValueOrDefault((u.Wojewodztwo, u.Powiat, u.Gmina, u.RodzajGminy))?.Nazwa,
                Miasto = miaDict.GetValueOrDefault(u.Symbol)
            }).ToList();


            foreach (var ulic in resultList)
            {
                przetworzono++;

                if (przetworzono % 50000 == 0)
                {
                    _logger.LogInfo($"Przetworzono {przetworzono}/{ulicData.Count} ulic...");
                }

                var kodPowiatu = ulic.Ulica.Wojewodztwo + ulic.Ulica.Powiat;
                var powiatCode = ulic.Ulica.Powiat;
                var isCityWithPowiatRights = powiatCode == "61" || powiatCode == "62" ||
                                            powiatCode == "63" || powiatCode == "64" || powiatCode == "65";

                Miasto? miasto = null;

                if (isCityWithPowiatRights)
                {
                    if (miastaNaPrawachPowiatuDict.ContainsKey(kodPowiatu))
                    {
                        miasto = miastaNaPrawachPowiatuDict[kodPowiatu];
                        cityWithRightsProcessed++;
                    }
                    else
                    {
                        if (brakujacych < 10)
                        {
                            _logger.LogWarning($"Brak mapowania dla miasta na prawach powiatu: kod powiatu={kodPowiatu}, ulica={ulic.Ulica.Nazwa1}");
                        }
                        brakujacych++;
                        continue;
                    }
                }
                else
                {
                    if (miastoDict.ContainsKey(ulic.Ulica.Symbol))
                    {
                        miasto = miastoDict[ulic.Ulica.Symbol];
                        regularProcessed++;
                    }
                    else
                    {
                        brakujacych++;
                        continue;
                    }
                }

                string? dzielnica = null;
                string? tempNazwa1 = ulic.Ulica.Nazwa1; // Tymczasowa zmienna dla obliczeń
                string? tempNazwa2 = ulic.Ulica.Nazwa2;
                string? Cecha = ulic.Ulica.Cecha;

                // 🔄 KROK 1: Zastosuj wstępne transformacje
                dzielnica = UliceUtils.Wesola(ulic);
                if (dzielnica == "")
                {
                    (tempNazwa1, dzielnica) = UliceUtils.ZielonaGora(miasto, tempNazwa1, dzielnica);
                }


                // ✅ ZMIENIONO: Używamy tempNazwa1 i tempNazwa2 do wyszukiwania w słowniku
                var originalParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Cecha))
                    originalParts.Add(Cecha.Trim());
                if (!string.IsNullOrWhiteSpace(tempNazwa2))
                    originalParts.Add(tempNazwa2.Trim());
                if (!string.IsNullOrWhiteSpace(tempNazwa1))
                    originalParts.Add(tempNazwa1.Trim());


                var original = string.Join(" ", originalParts);

                var ulica = new Ulica
                {
                    Symbol = ulic.Ulica.SymbolUlicy,
                    Cecha = Cecha,
                    MiastoId = miasto.Id,
                    Dzielnica = dzielnica,
                    TypUlicyId = null // Domyślnie null
                };

                if (terytUlicPoprawkiDict.TryGetValue(original, out var terytUlicPoprawka))
                {
                    // Cecha z poprawek staje się cechą ulicy
                    ulica.Cecha = terytUlicPoprawka.Cecha;
                    // Znaleziono w słowniku - spróbuj znaleźć odpowiedni TypUlicy w bazie

                    var key = new TypUlicyKey
                    {
                        Prefiks = terytUlicPoprawka.Prefiks ?? "",
                        Tytul = terytUlicPoprawka.Tytul ?? "",
                        Imie = terytUlicPoprawka.Imie ?? "",
                        Imie2 = terytUlicPoprawka.Imie2 ?? "",
                        Nazwisko = terytUlicPoprawka.Nazwisko ?? "",
                        Nazwisko2 = terytUlicPoprawka.Nazwisko2 ?? "",
                        Pseudonim = terytUlicPoprawka.Pseudonim ?? "",
                        Postfiks = terytUlicPoprawka.Postfiks ?? ""
                    };

                    if (typyUlicDict.TryGetValue(key, out var typUlicyId))
                    {
                        ulica.TypUlicyId = typUlicyId;
                        typUlicyAssigned++;
                    }
                }

                allUlice.Add(ulica);
            }

            _logger.LogInfo($"Zebrano {allUlice.Count} ulic");
            _logger.LogInfo($"Przypisano TypUlicyId dla {typUlicyAssigned} ulic");
            _logger.LogInfo("Usuwam duplikaty (Symbol + Dzielnica + MiastoId)...");

            // Usuń duplikaty
            var uniqueUlice = allUlice
                .GroupBy(u => new { u.Symbol, u.Dzielnica, u.MiastoId })
                .Select(g => g.First())
                .ToList();

            int duplikaty = allUlice.Count - uniqueUlice.Count;
            int dodano = uniqueUlice.Count;

            _logger.LogInfo($"Po usunięciu duplikatów: {uniqueUlice.Count} unikalnych ulic (pominięto {duplikaty} duplikatów)");
            _logger.LogInfo("Zapisuję do bazy danych...");

            // Wstaw wszystkie ulice jednym ruchem
            await _context.Ulice.AddRangeAsync(uniqueUlice);
            await _context.SaveChangesAsync();

            _logger.LogInfo("=== Podsumowanie ładowania ulic ===");
            _logger.LogInfo($"Przetworzono: {przetworzono}");
            _logger.LogInfo($"Dodano: {dodano}");
            _logger.LogInfo($"  - Dla miast na prawach powiatu: {cityWithRightsProcessed}");
            _logger.LogInfo($"  - Dla zwykłych miejscowości: {regularProcessed}");
            _logger.LogInfo($"  - Skonwertowano z Excel: {convertedFromExcel}");
            _logger.LogInfo($"  - Zmieniono prefiksy: {prefixChanges}"); // 🆕 DODANE
            _logger.LogInfo($"Pominięto (brak miejscowości): {brakujacych}");
            _logger.LogInfo($"Pominięto (duplikaty): {duplikaty}");
        }

        public void Dispose()
        {
            _logger?.Dispose();
            _prefixLogger?.Dispose();
        }
    }

    /// <summary>
    /// Klucz do wyszukiwania TypUlicy (wszystkie pola oprócz Id)
    /// </summary>
    internal class TypUlicyKey
    {
        public string Prefiks { get; set; } = "";
        public string Tytul { get; set; } = "";
        public string Imie { get; set; } = "";
        public string Imie2 { get; set; } = "";
        public string Nazwisko { get; set; } = "";
        public string Nazwisko2 { get; set; } = "";
        public string Pseudonim { get; set; } = "";
        public string Postfiks { get; set; } = "";
    }

    /// <summary>
    /// Comparer dla TypUlicyKey
    /// </summary>
    internal class TypUlicyKeyEqualityComparer : IEqualityComparer<TypUlicyKey>
    {
        public bool Equals(TypUlicyKey? x, TypUlicyKey? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Prefiks == y.Prefiks &&
                   x.Tytul == y.Tytul &&
                   x.Imie == y.Imie &&
                   x.Imie2 == y.Imie2 &&
                   x.Nazwisko == y.Nazwisko &&
                   x.Nazwisko2 == y.Nazwisko2 &&
                   x.Pseudonim == y.Pseudonim &&
                   x.Postfiks == y.Postfiks;
        }

        public int GetHashCode(TypUlicyKey obj)
        {
            return HashCode.Combine(
                obj.Prefiks,
                obj.Tytul,
                obj.Imie,
                obj.Imie2,
                obj.Nazwisko,
                obj.Nazwisko2,
                HashCode.Combine(obj.Pseudonim, obj.Postfiks)
            );
        }
    }
}