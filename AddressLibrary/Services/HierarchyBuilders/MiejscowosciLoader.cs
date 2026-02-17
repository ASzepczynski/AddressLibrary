using AddressLibrary.Data;
using AddressLibrary.Logging;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class MiastaLoader : IDisposable
    {
        private readonly AddressDbContext _context;
        private readonly HierarchyLogger _logger;

        public MiastaLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _logger = new HierarchyLogger(appDataPath);
        }

        public async Task<Dictionary<string, Miasto>> LoadAsync(
            List<TerytSimc> simcData,
            Dictionary<string, Gmina> gminyDict,
            Dictionary<string, RodzajMiasta> rodzajeMiasta)
        {
            var miastaDict = new Dictionary<string, Miasto>();

            _logger.LogInfo("Rekord domyślny 'Brak' z Id=-1 już istnieje (utworzony przez DefaultRecordSeeder)");

            int cityWithRightsCount = 0;
            int regularCount = 0;
            int notFoundGminaCount = 0;
            int skippedDelegaturesCount = 0;
            int skippedDistrictsCount = 0;

            // Zgrupuj miejscowści według gminy
            var miastaByGmina = simcData
                .GroupBy(s => new { s.Wojewodztwo, s.Powiat, s.Gmina, s.RodzajGminy })
                .ToList();

            _logger.LogInfo($"Liczba grup miejscowości według gmin: {miastaByGmina.Count}");

            foreach (var gminaGroup in miastaByGmina)
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
                        _logger.LogWarning($"Nie znaleziono gminy dla klucza: {kodGminy}");
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
                        int? rodzajMiastaId = null;
                        if (!string.IsNullOrEmpty(glowneMiasto.RodzajMiasta) && rodzajeMiasta.ContainsKey(glowneMiasto.RodzajMiasta))
                        {
                            rodzajMiastaId = rodzajeMiasta[glowneMiasto.RodzajMiasta].Id;
                        }

                        var miasto = new Miasto
                        {
                            Symbol = glowneMiasto.Symbol,
                            Nazwa = glowneMiasto.Nazwa,
                            RodzajMiastaId = rodzajMiastaId ?? -1,
                            GminaId = gmina.Id
                        };

                        miastaDict[glowneMiasto.Symbol] = miasto;
                        await _context.Miasta.AddAsync(miasto);
                        cityWithRightsCount++;

                        _logger.LogInfo($"Dodano miasto na prawach powiatu: {miasto.Nazwa}, Symbol: {miasto.Symbol}, Gmina: {gmina.Nazwa}");
                    }
                    else
                    {
                        _logger.LogWarning($"Brak miasta z rodzajem '96' dla gminy {gmina.Nazwa} (kod: {kodGminy})");
                    }
                }
                else
                {
                    // Dla zwykłych gmin - dodaj wszystkie miejscowości, ALE POMIŃ DZIELNICE
                    foreach (var simc in gminaGroup)
                    {
                        // FILTR: Pomiń dzielnice (miejscowości będące częścią innej miejscowości)
                        // Jeśli SymbolPodstawowy != Symbol, to jest to dzielnica

                        // Dla celów testowych ładujemy wszystko
                        if (false && simc.SymbolPodstawowy != simc.Symbol)
                        {
                            skippedDistrictsCount++;
                            if (skippedDistrictsCount <= 10) // Loguj tylko pierwsze 10
                            {
                                _logger.LogInfo($"Pominięto dzielnicę: {simc.Nazwa} (Symbol: {simc.Symbol}, SymbolPodstawowy: {simc.SymbolPodstawowy})");
                            }
                            continue;
                        }

                        if (!miastaDict.ContainsKey(simc.Symbol))
                        {
                            int? rodzajMiastaId = null;
                            if (!string.IsNullOrEmpty(simc.RodzajMiasta) && rodzajeMiasta.ContainsKey(simc.RodzajMiasta))
                            {
                                rodzajMiastaId = rodzajeMiasta[simc.RodzajMiasta].Id;
                            }

                            var miasto = new Miasto
                            {
                                Symbol = simc.Symbol,
                                Nazwa = simc.Nazwa,
                                RodzajMiastaId = rodzajMiastaId ?? -1,
                                GminaId = gmina.Id
                            };
                            miastaDict[simc.Symbol] = miasto;
                            await _context.Miasta.AddAsync(miasto);
                            regularCount++;
                        }
                    }
                }
            }

            _logger.LogInfo($"Dodano {cityWithRightsCount} miast na prawach powiatu (rodzaj '96')");
            _logger.LogInfo($"Dodano {regularCount} zwykłych miejscowości");
            if (skippedDistrictsCount > 0)
            {
                _logger.LogInfo($"Pominięto {skippedDistrictsCount} dzielnic (SymbolPodstawowy != Symbol)");
            }
            if (skippedDelegaturesCount > 0)
            {
                _logger.LogInfo($"Pominięto {skippedDelegaturesCount} delegatur/dzielnic (nie wymagają gminy - to OK)");
            }
            if (notFoundGminaCount > 0)
            {
                _logger.LogWarning($"Pominięto {notFoundGminaCount} grup (brak gminy w słowniku - wymaga uwagi)");
            }
            
            await _context.SaveChangesAsync();

            return miastaDict;
        }

        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
}