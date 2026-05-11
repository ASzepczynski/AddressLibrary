using AddressLibrary.Data;
using AddressLibrary.Dictionaries;
using AddressLibrary.Dictionaries.CechyUlic;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Structures;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class UliceLoader : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly HierarchyStreetLogger _logger;
        private readonly PrefixChangeLogger _prefixLogger;
        private readonly TytulyStopnieDictionaryService _tytulyService;
        private readonly TypyUlicDictionaryService _typyUlicService;
        private readonly CechyUlicDictionary _cechyUlicDict;

        public UliceLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new HierarchyStreetLogger(appDataPath);
            _prefixLogger = new PrefixChangeLogger(appDataPath);
            _tytulyService = new TytulyStopnieDictionaryService(context);
            _typyUlicService = new TypyUlicDictionaryService(context);
            _cechyUlicDict = new CechyUlicDictionary(context);
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

            // ✅ Załaduj słowniki za pomocą serwisów
            _logger.LogInfo("Wczytywanie słownika TerytUlicPoprawki...");
            var terytUlicPoprawkiDict = TerytUlicPoprawkiDictionary.Load(appDataPath, _logger);
            _logger.LogInfo($"Załadowano {terytUlicPoprawkiDict.Count} wpisów ze słownika TerytUlicPoprawki");

            _logger.LogInfo("Wczytywanie mapowania TypyUlic z bazy danych...");
            var typyUlicDict = await _typyUlicService.GetTypyUlicMappingAsync();
            _logger.LogInfo($"Załadowano {typyUlicDict.Count} wpisów z tabeli TypyUlic");

            // ✅ Zainicjalizuj słownik tytułów/stopni z bazy danych
            _logger.LogInfo("Wczytywanie słownika TytulyStopnie...");
            _tytulyService.ClearCache();
            await _tytulyService.GetSkrotToIdMappingAsync();
            await _tytulyService.GetDopelniaczToIdMappingAsync();
            _logger.LogInfo("Słownik TytulyStopnie został zainicjalizowany");

            // ✅ DODANO: Zainicjalizuj słownik CechyUlic
            _logger.LogInfo("Wczytywanie słownika CechyUlic...");
            var cechyUlicMapping = await _cechyUlicDict.GetSkrotToIdMappingAsync();
            _logger.LogInfo($"Załadowano {cechyUlicMapping.Count} wpisów ze słownika CechyUlic");

            int przetworzono = 0;
            int brakujacych = 0;
            int cityWithRightsProcessed = 0;
            int regularProcessed = 0;
            int convertedFromExcel = 0;
            int prefixChanges = 0;
            int typUlicyAssigned = 0;
            int cechyUlicAssigned = 0;

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
//                        _logger.LogInfo($"Zarejestrowano miasto na prawach powiatu: {miasto.Nazwa} (MiastoId={miasto.Id}), Gmina: {gmina.Nazwa} (GminaId={gmina.Id}), Powiat: {kodPowiatu}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Nie znaleziono miasta dla gminy {gmina.Nazwa} (GminaId={gmina.Id})");
                }
            }

            _logger.LogInfo($"Mapowanie miast na prawach powiatu zawiera {miastaNaPrawachPowiatuDict.Count} wpisów");

            //// Wyświetl wszystkie wpisy
            //foreach (var kvp in miastaNaPrawachPowiatuDict)
            //{
            //    _logger.LogInfo($"  [{kvp.Key}] => {kvp.Value.Nazwa} (MiastoId={kvp.Value.Id})");
            //}

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

            var brakujaceTytuly = new List<string>();
            // Zbiór unikalnych brakujących cech ulicy z poprawek
            var brakujaceCechy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var brakujaceUlice = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                string? tempNazwa1 = ulic.Ulica.Nazwa1;
                string? tempNazwa2 = ulic.Ulica.Nazwa2;
                string? Cecha = ulic.Ulica.Cecha;

                // 🔄 KROK 1: Zastosuj wstępne transformacje
                dzielnica = UliceUtils.Wesola(ulic);
                if (dzielnica == "")
                {
                    // Tutaj nie chcemy modyfikacji nazwy ulicy, bo chcemy potem w poprawkach TerytLoad mieć właściwą nazwę
                    (var tempNazwa3, dzielnica) = UliceUtils.ZielonaGora(miasto, tempNazwa1, dzielnica);
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
                    CechaUlicyId = -1,
                    MiastoId = miasto.Id,
                    Dzielnica = dzielnica,
                    TypUlicyId = -1
                };

                if (terytUlicPoprawkiDict.TryGetValue(original, out var terytUlicPoprawka))
                {
   
                    // ✅ ZMIENIONO: Użyj CechyUlicDictionary dla cechy z poprawek
                    string sCecha = terytUlicPoprawka.Cecha;
                    if (!string.IsNullOrWhiteSpace(sCecha))
                    {
                        var cUlicy = await _cechyUlicDict.FindByNazwaAsync(sCecha);
                        if (cUlicy == null)
                        {
                            // Zbieramy brakujące cechy do unikalnego zbioru - logujemy je dopiero po przetworzeniu
                            if (!string.IsNullOrWhiteSpace(sCecha) && sCecha != "inne")
                                brakujaceCechy.Add(sCecha.Trim());
                        }
                        else
                        {
                            ulica.CechaUlicyId = cUlicy.Id;
                            cechyUlicAssigned++;
                        }
                    }

                    // ✅ Użyj serwisu do mapowania tytułu
                    int tytulStopienId = _tytulyService.MapDopelniaczToId(terytUlicPoprawka.Tytul);

                    if (tytulStopienId == -2)
                    {
                        tytulStopienId = -1;
                        brakujaceTytuly.Add(terytUlicPoprawka.Tytul);

                    }

                    // ✅ Użyj serwisu do znalezienia TypUlicyId
                    var typUlicyId = await _typyUlicService.FindTypUlicyIdAsync(
                        terytUlicPoprawka.Prefiks,
                        tytulStopienId,
                        terytUlicPoprawka.Imie,
                        terytUlicPoprawka.Imie2,
                        terytUlicPoprawka.Nazwisko,
                        terytUlicPoprawka.Nazwisko2,
                        terytUlicPoprawka.Pseudonim,
                        terytUlicPoprawka.Postfiks
                    );

                    if (typUlicyId.HasValue)
                    {
                        ulica.TypUlicyId = typUlicyId.Value;
                        typUlicyAssigned++;
                    }
                } else
                {
                    _logger.LogError($"Nie znaleziono w TerytLoadPoprawki: '{original}'");
                    // Dodaj oryginalny ciąg do zbioru braków
                    if (!string.IsNullOrWhiteSpace(original))
                        brakujaceUlice.Add(original);
                }

                allUlice.Add(ulica);
            }




            foreach (var elem in brakujaceTytuly.Distinct())
            {
                _logger.LogError($"Brak stopnia/tytułu '{elem}'");
            }

            // Wypisz unikalne brakujące cechy ulicy (jeśli wystąpiły)
            foreach (var cecha in brakujaceCechy.OrderBy(x => x))
            {
                _logger.LogError($"Brak cechy ulicy w TerytUlicPoprawka [{cecha}]");
            }

            _logger.LogInfo($"Zebrano {allUlice.Count} ulic");
            _logger.LogInfo($"Przypisano TypUlicyId dla {typUlicyAssigned} ulic");
            _logger.LogInfo($"Przypisano CechaUlicyId dla {cechyUlicAssigned} ulic");
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
            _logger.LogInfo($"  - Zmieniono prefiksy: {prefixChanges}");
            _logger.LogInfo($"  - Przypisano CechaUlicyId: {cechyUlicAssigned}");
            _logger.LogInfo($"Pominięto (brak miejscowości): {brakujacych}");
            _logger.LogInfo($"Pominięto (duplikaty): {duplikaty}");

            // Eksport unikalnych brakujących wpisów do pliku Excel obok oryginalnego słownika
            try
            {
                if (brakujaceUlice != null && brakujaceUlice.Count > 0)
                {
                    var dictDir = Path.Combine(appDataPath, "AppData", "Dictionaries");
                    var outPath = Path.Combine(dictDir, "TerytUlicPoprawki_braki.xlsx");

                    var brakiList = new List<TerytUlicPoprawka>();
                    foreach (var original in brakujaceUlice)
                    {
                        (string cecha, string ulica) = CechyUlicUtils.SplitStreetPrefix(original);

                        var tuPoprawka = new TerytUlicPoprawka
                        {
                            Cecha = cecha,
                            Prefiks = null,
                            Tytul = null,
                            Imie = null,
                            Imie2 = null,
                            Nazwisko = null,
                            Nazwisko2 = null,
                            Pseudonim = null,
                            Postfiks = ulica,
                            TerytId = original
                        };
                        brakiList.Add(tuPoprawka);
                    }
                    var exporter = new AddressLibrary.Services.ExcelExportService();
                    await exporter.ExportToExcelAsync(brakiList, outPath, "TerytUlicPoprawki");
                    _logger.LogInfo($"Zapisano {brakiList.Count} unikalnych braków do: {outPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Błąd zapisu TerytUlicPoprawki_braki.xlsx: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _logger?.Dispose();
            _prefixLogger?.Dispose();
        }
    }
}