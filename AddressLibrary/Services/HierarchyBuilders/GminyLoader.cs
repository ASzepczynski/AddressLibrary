using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class GminyLoader
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;

        public GminyLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
        }

        public async Task<Dictionary<string, Gmina>> LoadAsync(
            List<TerytTerc> tercData,
            Dictionary<string, Powiat> powiatyDict,
            Dictionary<string, RodzajGminy> rodzajeGminDict)
        {
            var gminyDict = new Dictionary<string, Gmina>();

            // Wyciągnij unikalne gminy
            var gminyKody = tercData
                .Where(t => !string.IsNullOrEmpty(t.Gmina))
                .Select(t => new { t.Wojewodztwo, t.Powiat, t.Gmina, t.RodzajGminy })
                .Distinct()
                .ToList();

            // Pobierz istniejące kody z bazy (aby uniknąć duplikatów)
            var existingCodes = await _context.Gminy
                .Select(g => g.Kod)
                .ToListAsync();

            foreach (var gminaInfo in gminyKody)
            {
                var tercGmina = tercData.FirstOrDefault(t =>
                    t.Wojewodztwo == gminaInfo.Wojewodztwo &&
                    t.Powiat == gminaInfo.Powiat &&
                    t.Gmina == gminaInfo.Gmina &&
                    t.RodzajGminy == gminaInfo.RodzajGminy);

                if (tercGmina != null)
                {
                    // FILTROWANIE: Dla miast na prawach powiatu (kody 61-65) pomiń delegatury
                    var powiatCode = gminaInfo.Powiat;
                    var isCityWithPowiatRights = powiatCode == "61" || powiatCode == "62" || 
                                                powiatCode == "63" || powiatCode == "64" || powiatCode == "65";

                    // Dla miast na prawach powiatu - dodaj tylko gminę o rodzaju '1' lub '8' z kodem gminy '01'
                    // (główna gmina miejska, pomiń delegatury/dzielnice)
                    if (isCityWithPowiatRights)
                    {
                        // Pomiń delegatury - bierzemy tylko główną gminę (kod gminy zwykle '01')
                        // lub gminę miejską (rodzaj '1')
                        if (gminaInfo.Gmina != "01" && gminaInfo.RodzajGminy == "8")
                        {
                            // To jest delegatura, pomiń
                            continue;
                        }
                        
                        // Jeśli to rodzaj '1' (gmina miejska), to jest główna gmina
                        if (gminaInfo.RodzajGminy != "1" && gminaInfo.Gmina != "01")
                        {
                            // Pomiń jeśli to nie jest główna gmina
                            continue;
                        }
                    }

                    var powiatKey = $"{gminaInfo.Wojewodztwo}|{gminaInfo.Powiat}";
                    
                    if (powiatyDict.TryGetValue(powiatKey, out var powiat) &&
                        rodzajeGminDict.TryGetValue(gminaInfo.RodzajGminy, out var rodzajGminy))
                    {
                        var klucz = $"{gminaInfo.Wojewodztwo}|{gminaInfo.Powiat}|{gminaInfo.Gmina}|{gminaInfo.RodzajGminy}";
                        
                        // KLUCZOWA ZMIANA: Pełny kod 7-cyfrowy (woj + powiat + gmina + rodzaj)
                        var kodGminy = $"{gminaInfo.Wojewodztwo}{gminaInfo.Powiat}{gminaInfo.Gmina}{gminaInfo.RodzajGminy}";

                        // Pomiń jeśli kod już istnieje
                        if (existingCodes.Contains(kodGminy))
                        {
                            continue;
                        }

                        var gmina = new Gmina
                        {
                            Kod = kodGminy, // 7 cyfr: np. "0201011" zamiast "01"
                            Nazwa = tercGmina.Nazwa,
                            PowiatId = powiat.Id,
                            RodzajGminyId = rodzajGminy.Id
                        };
                        
                        gminyDict[klucz] = gmina;
                        await _context.Gminy.AddAsync(gmina);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return gminyDict;
        }
    }
}