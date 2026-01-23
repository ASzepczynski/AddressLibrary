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
            Dictionary<string, Miasto> miastoDict)
        {
            await LogControl("=== Rozpoczynam ładowanie ulic ===\n");
            await LogControl($"Liczba ulic do przetworzenia: {ulicData.Count}");
            await LogControl($"Liczba miejscowości w słowniku: {miastoDict.Count}");

            int przetworzono = 0;
            int brakujacych = 0;
            int cityWithRightsProcessed = 0;
            int regularProcessed = 0;

            // Dla miast na prawach powiatu - załaduj raz na początku
            await LogControl("Przygotowuję mapowanie miast na prawach powiatu...");
            var miastaNaPrawachPowiatuDict = new Dictionary<string, Miasto>();

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
                var miasto = miastoDict.Values.FirstOrDefault(m => m.GminaId == gmina.Id);

                if (miasto != null)
                {
                    if (!miastaNaPrawachPowiatuDict.ContainsKey(kodPowiatu))
                    {
                        miastaNaPrawachPowiatuDict[kodPowiatu] = miasto;
                        await LogControl($"Zarejestrowano miasto na prawach powiatu: {miasto.Nazwa} (MiastoId={miasto.Id}), Gmina: {gmina.Nazwa} (GminaId={gmina.Id}), Powiat: {kodPowiatu}");
                    }
                }
                else
                {
                    await LogControl($"⚠️ UWAGA: Nie znaleziono miasta dla gminy {gmina.Nazwa} (GminaId={gmina.Id})");
                }
            }

            await LogControl($"Mapowanie miast na prawach powiatu zawiera {miastaNaPrawachPowiatuDict.Count} wpisów");

            // Wyświetl wszystkie wpisy
            foreach (var kvp in miastaNaPrawachPowiatuDict)
            {
                await LogControl($"  [{kvp.Key}] => {kvp.Value.Nazwa} (MiastoId={kvp.Value.Id})");
            }

            await LogControl("Przetwarzam ulice...");

            // Lista wszystkich ulic do wstawienia
            var allUlice = new List<Ulica>(ulicData.Count);

            // Główna pętla - tylko przygotowanie danych

            var wojDict = _context.Wojewodztwa.AsNoTracking().ToDictionary(x => x.Kod);

            var powDict = _context.Powiaty.AsNoTracking().ToDictionary(x => x.Kod);

            var gmiDict = _context.TerytTerc.AsNoTracking().ToDictionary(x => (x.Wojewodztwo, x.Powiat, x.Gmina, x.RodzajGminy));

            var miaDict = _context.TerytSimc.AsNoTracking().ToDictionary(x => x.Symbol);

            var resultList = ulicData.Select(u => new
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
                    await LogControl($"Przetworzono {przetworzono}/{ulicData.Count} ulic...");
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
                            await LogControl($"⚠️ Brak mapowania dla miasta na prawach powiatu: kod powiatu={kodPowiatu}, ulica={ulic.Ulica.Nazwa1}");
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

                // Wyjątek dla Wesołej, dzielnicy Warszawy. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę
                if (ulic.WojewodztwoNazwa.ToLower() == "mazowieckie" && ulic.PowiatNazwa == "Warszawa" && ulic.GminaNazwa == "Wesoła" && ulic.Miasto?.Nazwa=="Wesoła" && ulic.Miasto.RodzajMiasta=="95")
                {
                    dzielnica = "Wesoła";
                }
                // Wyjątek dla Zielonej Góry. Nazwy ulic się powtarzają więc trzeba ustawić dzielnicę, która jest zawarta w nazwie ulicy.


                if (ulic.WojewodztwoNazwa.ToLower() == "lubuskie" && ulic.PowiatNazwa == "Zielona Góra" && ulic.GminaNazwa == "Zielona Góra" && ulic.Miasto?.Nazwa == "Zielona Góra")
                {
                    var dzielnice= new List<string> {
                        "Drzonków",
                        "Kiełpin",
                        "Kisielin",
                        "Krępa",
                        "Łężyca",
                        "Ługowo",
                        "Nowy Kisielin",
                        "Ochla",
                        "Przylep",
                        "Racula",
                        "Stary Kisielin",
                        "Zatonie",
                        "Zawada"
                    };


                    foreach (var dziel in dzielnice) {
                        if (ulic.Ulica.Nazwa1.StartsWith(dziel + "-"))
                        {
                            dzielnica = dziel;
                            Nazwa1=ulic.Ulica.Nazwa1.Remove(0,dziel.Length+1);
                            break;
                        }
                    }
                }

                var ulica = new Ulica
                {
                    Symbol = ulic.Ulica.SymbolUlicy,
                    Cecha = ulic.Ulica.Cecha,
                    Nazwa1 = Nazwa1,
                    Nazwa2 = ulic.Ulica.Nazwa2,
                    MiastoId = miasto.Id,
                    Dzielnica = dzielnica
                };

                allUlice.Add(ulica);
            }

            await LogControl($"\nZebrano {allUlice.Count} ulic");
            await LogControl("Usuwam duplikaty (Symbol + Dzielnica + MiastoId)...");

            // ✅ ZMIENIONO: Usuń duplikaty po Symbol + Dzielnica + MiastoId
            var uniqueUlice = allUlice
                .GroupBy(u => new { u.Symbol, u.Dzielnica, u.MiastoId })
                .Select(g => g.First())
                .ToList();

            int duplikaty = allUlice.Count - uniqueUlice.Count;
            int dodano = uniqueUlice.Count;

            await LogControl($"Po usunięciu duplikatów: {uniqueUlice.Count} unikalnych ulic (pominięto {duplikaty} duplikatów)");

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