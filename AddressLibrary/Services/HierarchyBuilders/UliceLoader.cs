using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class UliceLoader
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;
        private readonly string _controlLogPath;

        public UliceLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
            
            var logsDir = Path.Combine(appDataPath ?? AppDomain.CurrentDomain.BaseDirectory, "AppData", "Logs");
            Directory.CreateDirectory(logsDir);
            _controlLogPath = Path.Combine(logsDir, "Control.txt");
        }

        public async Task LoadAsync(
            List<TerytUlic> ulicData,
            Dictionary<string, Miejscowosc> miejscowosciDict)
        {
            await LogControl("=== Rozpoczynam ładowanie ulic ===\n");
            await LogControl($"Liczba ulic do przetworzenia: {ulicData.Count}");
            await LogControl($"Liczba miejscowości w słowniku: {miejscowosciDict.Count}");

            int przetworzono = 0;
            int brakujacych = 0;
            int cityWithRightsProcessed = 0;
            int regularProcessed = 0;

            // Dla miast na prawach powiatu - załaduj raz na początku
            await LogControl("Przygotowuję mapowanie miast na prawach powiatu...");
            var miastaNaPrawachPowiatuDict = new Dictionary<string, Miejscowosc>();

            // Załaduj wszystkie gminy z powiatami
            var gminyAll = await _context.Gminy
                .Include(g => g.Powiat)
                    .ThenInclude(p => p.Wojewodztwo)
                .ToListAsync();

            await LogControl($"Załadowano {gminyAll.Count} gmin z bazy");

            // POPRAWKA: Filtruj gminy w miastach na prawach powiatu
            // Powiat.Kod jest teraz 4-cyfrowy (np. "2261"), więc sprawdzamy końcówkę
            var gminyWMiastachNaPrawachPowiatu = gminyAll
                .Where(g => g.Powiat.Kod.EndsWith("61") || g.Powiat.Kod.EndsWith("62") || 
                           g.Powiat.Kod.EndsWith("63") || g.Powiat.Kod.EndsWith("64") || 
                           g.Powiat.Kod.EndsWith("65"))
                .ToList();

            await LogControl($"Znaleziono {gminyWMiastachNaPrawachPowiatu.Count} gmin w miastach na prawach powiatu");

            foreach (var gmina in gminyWMiastachNaPrawachPowiatu)
            {
                // Klucz to pełny 4-cyfrowy kod powiatu (już jest w gmina.Powiat.Kod)
                var kodPowiatu = gmina.Powiat.Kod; // np. "2261"
                var miejscowosc = miejscowosciDict.Values.FirstOrDefault(m => m.GminaId == gmina.Id);
                
                if (miejscowosc != null)
                {
                    if (!miastaNaPrawachPowiatuDict.ContainsKey(kodPowiatu))
                    {
                        miastaNaPrawachPowiatuDict[kodPowiatu] = miejscowosc;
                        await LogControl($"Zarejestrowano miasto na prawach powiatu: {miejscowosc.Nazwa} (MiejscowoscId={miejscowosc.Id}), Gmina: {gmina.Nazwa} (GminaId={gmina.Id}), Powiat: {kodPowiatu}");
                    }
                }
                else
                {
                    await LogControl($"⚠️ UWAGA: Nie znaleziono miejscowości dla gminy {gmina.Nazwa} (GminaId={gmina.Id})");
                }
            }

            await LogControl($"Mapowanie miast na prawach powiatu zawiera {miastaNaPrawachPowiatuDict.Count} wpisów");
            
            // Wyświetl wszystkie wpisy
            foreach (var kvp in miastaNaPrawachPowiatuDict)
            {
                await LogControl($"  [{kvp.Key}] => {kvp.Value.Nazwa} (MiejscowoscId={kvp.Value.Id})");
            }
            
            await LogControl("Przetwarzam ulice...");

            // Lista wszystkich ulic do wstawienia
            var allUlice = new List<Ulica>(ulicData.Count);

            // Diagnostyka dla Gdańska
            int gdanskUliceCount = 0;

            // Główna pętla - tylko przygotowanie danych
            foreach (var ulic in ulicData)
            {
                przetworzono++;

                if (przetworzono % 50000 == 0)
                {
                    await LogControl($"Przetworzono {przetworzono}/{ulicData.Count} ulic...");
                }

                // POPRAWKA: Buduj 4-cyfrowy kod powiatu
                var kodPowiatu = ulic.Wojewodztwo + ulic.Powiat; // np. "2261"
                var powiatCode = ulic.Powiat; // 2 cyfry, np. "61"
                var isCityWithPowiatRights = powiatCode == "61" || powiatCode == "62" || 
                                            powiatCode == "63" || powiatCode == "64" || powiatCode == "65";

                Miejscowosc? miejscowosc = null;

                if (isCityWithPowiatRights)
                {
                    if (miastaNaPrawachPowiatuDict.ContainsKey(kodPowiatu))
                    {
                        miejscowosc = miastaNaPrawachPowiatuDict[kodPowiatu];
                        cityWithRightsProcessed++;

                        // Diagnostyka dla Gdańska (22 = Pomorskie, 61 = Gdańsk)
                        if (kodPowiatu == "2261")
                        {
                            gdanskUliceCount++;
                            if (gdanskUliceCount <= 5)
                            {
                                await LogControl($"  Gdańsk: ulica {ulic.Nazwa1} => MiejscowoscId={miejscowosc.Id}");
                            }
                        }
                    }
                    else
                    {
                        // Loguj pierwsze nieznalezione miasta
                        if (brakujacych < 10)
                        {
                            await LogControl($"⚠️ Brak mapowania dla miasta na prawach powiatu: kod powiatu={kodPowiatu}, ulica={ulic.Nazwa1}");
                        }
                        brakujacych++;
                        continue;
                    }
                }
                else
                {
                    if (miejscowosciDict.ContainsKey(ulic.Symbol))
                    {
                        miejscowosc = miejscowosciDict[ulic.Symbol];
                        regularProcessed++;
                    }
                    else
                    {
                        brakujacych++;
                        continue;
                    }
                }

                // Dodaj ulicę do listy
                var ulica = new Ulica
                {
                    Symbol = ulic.SymbolUlicy,
                    Cecha = ulic.Cecha,
                    Nazwa1 = ulic.Nazwa1,
                    Nazwa2 = ulic.Nazwa2,
                    MiejscowoscId = miejscowosc.Id
                };

                allUlice.Add(ulica);
            }

            await LogControl($"Zebrano {allUlice.Count} ulic");
            await LogControl($"W tym dla Gdańska: {gdanskUliceCount} ulic");
            await LogControl("Usuwam duplikaty (Symbol + MiejscowoscId)...");

            // Usuń duplikaty - ulice o tym samym Symbol w tej samej miejscowości
            var uniqueUlice = allUlice
                .GroupBy(u => new { u.Symbol, u.MiejscowoscId })
                .Select(g => g.First())
                .ToList();

            int duplikaty = allUlice.Count - uniqueUlice.Count;
            int dodano = uniqueUlice.Count;

            await LogControl($"Po usunięciu duplikatów: {uniqueUlice.Count} unikalnych ulic (pominięto {duplikaty} duplikatów)");
            
            // Sprawdź ile ulic dla Gdańska po deduplikacji
            var gdanskMiejscowosc = miastaNaPrawachPowiatuDict.GetValueOrDefault("2261");
            if (gdanskMiejscowosc != null)
            {
                var gdanskUliceUnique = uniqueUlice.Count(u => u.MiejscowoscId == gdanskMiejscowosc.Id);
                await LogControl($"Po deduplikacji dla Gdańska: {gdanskUliceUnique} unikalnych ulic");
            }

            await LogControl("Zapisuję do bazy danych...");

            // Wstaw wszystkie ulice jednym ruchem
            await _context.Ulice.AddRangeAsync(uniqueUlice);
            await _context.SaveChangesAsync();

            await LogControl($"\n=== Podsumowanie ładowania ulic ===");
            await LogControl($"Przetworzono: {przetworzono}");
            await LogControl($"Dodano: {dodano}");
            await LogControl($"  - Dla miast na prawach powiatu: {cityWithRightsProcessed}");
            await LogControl($"  - Dla zwykłych miejscowości: {regularProcessed}");
            await LogControl($"Pominięto (brak miejscowości): {brakujacych}");
            await LogControl($"Pominięto (duplikaty): {duplikaty}");
        }

        private async Task LogControl(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
                await File.AppendAllTextAsync(_controlLogPath, logEntry);
            }
            catch
            {
                // Ignoruj błędy zapisu do logu
            }
        }
    }
}