using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class MiejscowosciLoader
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;
        private readonly string _controlLogPath;

        public MiejscowosciLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
            
            // Ustaw ścieżkę do pliku kontrolnego
            var logsDir = Path.Combine(appDataPath ?? AppDomain.CurrentDomain.BaseDirectory, "AppData", "Logs");
            Directory.CreateDirectory(logsDir);
            _controlLogPath = Path.Combine(logsDir, "Control.txt");
        }

        public async Task<Dictionary<string, Miejscowosc>> LoadAsync(
            List<TerytSimc> simcData,
            Dictionary<string, Gmina> gminyDict,
            Dictionary<string, RodzajMiejscowosci> rodzajeMiejscowosci)
        {
            var miejscowosciDict = new Dictionary<string, Miejscowosc>();

            // Wyczyść poprzedni log kontrolny
            await File.WriteAllTextAsync(_controlLogPath, $"=== Log kontrolny budowania miejscowości - {DateTime.Now} ===\n\n");

            await LogControl("Rekord domyślny 'Brak' z Id=-1 już istnieje (utworzony przez DefaultRecordSeeder)");

            int cityWithRightsCount = 0;
            int regularCount = 0;
            int notFoundGminaCount = 0;
            int skippedDelegaturesCount = 0;
            int skippedDistrictsCount = 0;

            // Zgrupuj miejscowści według gminy
            var miejscowosciByGmina = simcData
                .GroupBy(s => new { s.Wojewodztwo, s.Powiat, s.Gmina, s.RodzajGminy })
                .ToList();

            await LogControl($"Liczba grup miejscowości według gmin: {miejscowosciByGmina.Count}");

            foreach (var gminaGroup in miejscowosciByGmina)
            {
                // POPRAWIONO: Użyj tego samego formatu klucza co w GminyLoader (z separatorami |)
                var kodGminy = $"{gminaGroup.Key.Wojewodztwo}|{gminaGroup.Key.Powiat}|{gminaGroup.Key.Gmina}|{gminaGroup.Key.RodzajGminy}";

                if (!gminyDict.ContainsKey(kodGminy))
                {
                    // Sprawdź czy to delegatura miasta na prawach powiatu (pomiń logowanie)
                    var powiatCode = gminaGroup.Key.Powiat;
                    var isCityWithPowiatRights = powiatCode == "61" || powiatCode == "62" || 
                                                powiatCode == "63" || powiatCode == "64" || powiatCode == "65";
                    
                    if (isCityWithPowiatRights && gminaGroup.Key.RodzajGminy == "8")
                    {
                        // To jest delegatura - pominięta w GminyLoader, nie loguj błędu
                        skippedDelegaturesCount++;
                    }
                    else if (isCityWithPowiatRights && gminaGroup.Key.RodzajGminy == "9")
                    {
                        // To jest delegatura typu 9 - też pominięta, nie loguj
                        skippedDelegaturesCount++;
                    }
                    else
                    {
                        notFoundGminaCount++;
                        await LogControl($"⚠️ UWAGA: Nie znaleziono gminy dla klucza: {kodGminy}");
                    }
                    continue;
                }

                var gmina = gminyDict[kodGminy];

                // Sprawdź czy to miasto na prawach powiatu (kod powiatu 61-65)
                var powiatCodeForCity = gminaGroup.Key.Powiat;
                var isCityWithPowiatRightsForCity = powiatCodeForCity == "61" || powiatCodeForCity == "62" || 
                                            powiatCodeForCity == "63" || powiatCodeForCity == "64" || powiatCodeForCity == "65";

                if (isCityWithPowiatRightsForCity)
                {
                    // Dla miast na prawach powiatu - dodaj TYLKO miasto z rodzajem '96'
                    var glowneMiasto = gminaGroup.FirstOrDefault(s => s.RodzajMiasta == "96");

                    if (glowneMiasto != null)
                    {
                        int? rodzajMiejscowosciId = null;
                        if (!string.IsNullOrEmpty(glowneMiasto.RodzajMiasta) && rodzajeMiejscowosci.ContainsKey(glowneMiasto.RodzajMiasta))
                        {
                            rodzajMiejscowosciId = rodzajeMiejscowosci[glowneMiasto.RodzajMiasta].Id;
                        }

                        var miejscowosc = new Miejscowosc
                        {
                            Symbol = glowneMiasto.Symbol,
                            Nazwa = glowneMiasto.Nazwa,
                            RodzajMiejscowosciId = rodzajMiejscowosciId ?? -1,
                            GminaId = gmina.Id
                        };

                        miejscowosciDict[glowneMiasto.Symbol] = miejscowosc;
                        await _context.Miejscowosci.AddAsync(miejscowosc);
                        cityWithRightsCount++;

                        await LogControl($"Dodano miasto na prawach powiatu: {miejscowosc.Nazwa}, Symbol: {miejscowosc.Symbol}, Gmina: {gmina.Nazwa}");
                    }
                    else
                    {
                        await LogControl($"⚠️ UWAGA: Brak miasta z rodzajem '96' dla gminy {gmina.Nazwa} (kod: {kodGminy})");
                    }
                }
                else
                {
                    // Dla zwykłych gmin - dodaj wszystkie miejscowości, ALE POMIŃ DZIELNICE
                    foreach (var simc in gminaGroup)
                    {
                        // FILTR: Pomiń dzielnice (miejscowości będące częścią innej miejscowości)
                        // Jeśli SymbolPodstawowy != Symbol, to jest to dzielnica
                        if (simc.SymbolPodstawowy != simc.Symbol)
                        {
                            skippedDistrictsCount++;
                            if (skippedDistrictsCount <= 10) // Loguj tylko pierwsze 10
                            {
                                await LogControl($"Pominięto dzielnicę: {simc.Nazwa} (Symbol: {simc.Symbol}, SymbolPodstawowy: {simc.SymbolPodstawowy})");
                            }
                            continue;
                        }

                        if (!miejscowosciDict.ContainsKey(simc.Symbol))
                        {
                            int? rodzajMiejscowosciId = null;
                            if (!string.IsNullOrEmpty(simc.RodzajMiasta) && rodzajeMiejscowosci.ContainsKey(simc.RodzajMiasta))
                            {
                                rodzajMiejscowosciId = rodzajeMiejscowosci[simc.RodzajMiasta].Id;
                            }

                            var miejscowosc = new Miejscowosc
                            {
                                Symbol = simc.Symbol,
                                Nazwa = simc.Nazwa,
                                RodzajMiejscowosciId = rodzajMiejscowosciId ?? -1,
                                GminaId = gmina.Id
                            };
                            miejscowosciDict[simc.Symbol] = miejscowosc;
                            await _context.Miejscowosci.AddAsync(miejscowosc);
                            regularCount++;
                        }
                    }
                }
            }

            await LogControl($"Dodano {cityWithRightsCount} miast na prawach powiatu (rodzaj '96')");
            await LogControl($"Dodano {regularCount} zwykłych miejscowości");
            if (skippedDistrictsCount > 0)
            {
                await LogControl($"Pominięto {skippedDistrictsCount} dzielnic (SymbolPodstawowy != Symbol)");
            }
            if (skippedDelegaturesCount > 0)
            {
                await LogControl($"Pominięto {skippedDelegaturesCount} delegatur/dzielnic (nie wymagają gminy - to OK)");
            }
            if (notFoundGminaCount > 0)
            {
                await LogControl($"⚠️ Pominięto {notFoundGminaCount} grup (brak gminy w słowniku - wymaga uwagi)");
            }
            
            await _context.SaveChangesAsync();

            return miejscowosciDict;
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