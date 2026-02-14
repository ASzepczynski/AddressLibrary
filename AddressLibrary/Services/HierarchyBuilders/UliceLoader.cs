using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Structures;
using AddressLibrary.Helpers;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class UliceLoader : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly HierarchyStreetLogger _logger;
        private readonly PrefixChangeLogger _prefixLogger; // 🆕 DODANE
        private readonly StreetNamePersonalConverter _personalConverter;

        public UliceLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new HierarchyStreetLogger(appDataPath);
            _prefixLogger = new PrefixChangeLogger(appDataPath); // 🆕 DODANE
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

        public async Task LoadAsync(
            List<TerytUlic> ulicData,
            Dictionary<string, Miasto> miastoDict)
        {
            _logger.LogInfo($"Liczba ulic do przetworzenia: {ulicData.Count}");
            _logger.LogInfo($"Liczba miejscowości w słowniku: {miastoDict.Count}");

            int przetworzono = 0;
            int brakujacych = 0;
            int cityWithRightsProcessed = 0;
            int regularProcessed = 0;
            int convertedFromExcel = 0;
            int prefixChanges = 0; // 🆕 DODANE - licznik zmian prefiksów

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
            // Powiat.Kod jest teraz 4-cyfrowy (np. "2261"), więc sprawdzamy końcówkę
            var gminyWMiastachNaPrawachPowiatu = gminyAll
                .Where(g => g.Powiat.Kod.EndsWith("61") || g.Powiat.Kod.EndsWith("62") ||
                           g.Powiat.Kod.EndsWith("63") || g.Powiat.Kod.EndsWith("64") ||
                           g.Powiat.Kod.EndsWith("65"))
                .ToList();

            _logger.LogInfo($"Znaleziono {gminyWMiastachNaPrawachPowiatu.Count} gmin w miastach na prawach powiatu");

            foreach (var gmina in gminyWMiastachNaPrawachPowiatu)
            {
                // Klucz to pełny 4-cyfrowy kod powiatu (już jest w gmina.Powiat.Kod)
                var kodPowiatu = gmina.Powiat.Kod; // np. "2261"
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

                // POPRAWKA: Buduj 4-cyfrowy kod powiatu
                var kodPowiatu = ulic.Ulica.Wojewodztwo + ulic.Ulica.Powiat; // np. "2261"
                var powiatCode = ulic.Ulica.Powiat; // 2 cyfry, np. "61"
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
                        // Loguj pierwsze nieznalezione miasta
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
                string? Nazwa1 = ulic.Ulica.Nazwa1;
                string? Nazwa2 = ulic.Ulica.Nazwa2;
                string? Cecha = ulic.Ulica.Cecha;

                // 🔄 KROK 1: Zastosuj wstępne transformacje
                dzielnica = UliceUtils.Wesola(ulic);
                if (dzielnica == "")
                {
                    (Nazwa1, dzielnica) = UliceUtils.ZielonaGora(miasto, Nazwa1, dzielnica);
                }

                (Nazwa1, Nazwa2) = UliceUtils.GetCorrectedStreetName(Nazwa1, Nazwa2);

                // Tutaj usuwamy duplikaty
                Nazwa1 = UliceUtils.RemoveStreetTypeDuplication(Cecha, Nazwa1);

                // 🆕 KROK 1.5: Sprawdź czy Nazwa1 zaczyna się od prefiksu i przenieś go do Cecha
                var (changedPrefix, extractedPrefix, cleanedName) = ExtractPrefixFromName(Nazwa1);
                
                if (changedPrefix)
                {
                    var oldCecha = Cecha;
                    var oldNazwa1 = Nazwa1;
                    
                    Cecha = extractedPrefix;
                    Nazwa1 = cleanedName;
                    
                    prefixChanges++;
                    
                    // Loguj zmianę
                    _prefixLogger.LogPrefixChange(
                        oldCecha ?? "(brak)", 
                        oldNazwa1, 
                        Cecha, 
                        Nazwa1, 
                        miasto?.Nazwa ?? "?" 
                    );
                }

                // 🆕 KROK 2: sprawdź konwersję z Excel
                if (_personalConverter.TryConvert(
                    Cecha,
                    Nazwa1,
                    Nazwa2,
                    out var convertedCecha,
                    out var convertedNazwa1,
                    out var convertedNazwa2))
                {
                    Cecha = convertedCecha;
                    Nazwa1 = convertedNazwa1;
                    Nazwa2 = convertedNazwa2;
                    convertedFromExcel++;
                }

                var ulica = new Ulica
                {
                    Symbol = ulic.Ulica.SymbolUlicy,
                    Cecha = Cecha,
                    Nazwa1 = Nazwa1,
                    Nazwa2 = Nazwa2,
                    MiastoId = miasto.Id,
                    Dzielnica = dzielnica
                };

                // 🆕 KROK 3: Obsługa specjalnego przypadku "Most"
                // Jeśli cecha nie jest "most", a Nazwa1 zaczyna się od "Most ", to przenieś do cechy
                if (string.Equals(ulica.Cecha, "inne", StringComparison.OrdinalIgnoreCase) &&
                    ulica.Nazwa1.StartsWith("Most ", StringComparison.OrdinalIgnoreCase))
                {
                    var oldCecha = ulica.Cecha;
                    var oldNazwa1 = ulica.Nazwa1;

                    ulica.Cecha = "most";
                    ulica.Nazwa1 = ulica.Nazwa1.Substring(5).Trim(); // Usuń "Most " (5 znaków)

                    prefixChanges++;

                    // Loguj zmianę
                    _prefixLogger.LogPrefixChange(
                        oldCecha ?? "(brak)",
                        oldNazwa1,
                        ulica.Cecha,
                        ulica.Nazwa1,
                        miasto?.Nazwa ?? "?"
                    );

                    _logger.LogInfo($"[Most] Zmieniono: '{oldCecha ?? "(brak)"}' '{oldNazwa1}' → 'most' '{ulica.Nazwa1}' w {miasto?.Nazwa}");
                }

                allUlice.Add(ulica);
            }

            _logger.LogInfo($"Zebrano {allUlice.Count} ulic");
            _logger.LogInfo("Usuwam duplikaty (Symbol + Dzielnica + MiastoId)...");

            // ✅ ZMIENIONO: Usuń duplikaty po Symbol + Dzielnica + MiastoId
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

        /// <summary>
        /// 🆕 Sprawdza czy Nazwa1 zaczyna się od prefiksu i wyodrębnia go
        /// </summary>
        /// <returns>Tuple (czy zmieniono, nowy prefix, oczyszczona nazwa)</returns>
        private (bool changed, string? prefix, string cleanedName) ExtractPrefixFromName(string nazwa1)
        {
            if (string.IsNullOrWhiteSpace(nazwa1))
                return (false, null, nazwa1);

            // Użyj istniejącej metody SplitStreetPrefix z UliceUtils
            var (extractedPrefix, remainingName) = UliceUtils.SplitStreetPrefix(nazwa1);

            // Jeśli znaleziono prefix (nie jest pusty)
            if (!string.IsNullOrEmpty(extractedPrefix) && !string.IsNullOrEmpty(remainingName))
            {
                return (true, extractedPrefix, remainingName);
            }

            return (false, null, nazwa1);
        }

        public void Dispose()
        {
            _logger?.Dispose();
            _prefixLogger?.Dispose(); // 🆕 DODANE
        }
    }
}