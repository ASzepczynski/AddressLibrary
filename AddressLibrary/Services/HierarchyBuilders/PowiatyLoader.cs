using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class PowiatyLoader
    {
        private readonly AddressDbContext _context;

        public PowiatyLoader(AddressDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, Powiat>> LoadAsync(
            List<TerytTerc> tercData,
            Dictionary<string, Wojewodztwo> wojewodztwaDict)
        {
            var powiatyDict = new Dictionary<string, Powiat>();

            // Wyci¹gnij unikalne powiaty (bez kodu "00" - to jest "Brak")
            var powiatyKody = tercData
                .Where(t => !string.IsNullOrEmpty(t.Powiat) && 
                           t.Powiat != "00" &&
                           string.IsNullOrEmpty(t.Gmina))
                .Select(t => new { t.Wojewodztwo, t.Powiat })
                .Distinct()
                .ToList();

            // Pobierz istniej¹ce kody z bazy (aby unikn¹æ duplikatów)
            var existingCodes = await _context.Powiaty
                .Select(p => p.Kod)
                .ToListAsync();

            foreach (var powiatInfo in powiatyKody)
            {
                var tercPow = tercData.FirstOrDefault(t =>
                    t.Wojewodztwo == powiatInfo.Wojewodztwo &&
                    t.Powiat == powiatInfo.Powiat &&
                    t.Gmina == "");

                if (tercPow != null && wojewodztwaDict.TryGetValue(powiatInfo.Wojewodztwo, out var wojewodztwo))
                {
                    var klucz = $"{powiatInfo.Wojewodztwo}|{powiatInfo.Powiat}";
                    
                    // KLUCZOWA ZMIANA: Pe³ny kod 4-cyfrowy (województwo + powiat)
                    var kodPowiatu = $"{powiatInfo.Wojewodztwo}{powiatInfo.Powiat}";

                    // Pomiñ jeœli kod ju¿ istnieje
                    if (existingCodes.Contains(kodPowiatu))
                    {
                        continue;
                    }

                    var powiat = new Powiat
                    {
                        Kod = kodPowiatu, // 4 cyfry: np. "0201" zamiast "01"
                        Nazwa = tercPow.Nazwa,
                        WojewodztwoId = wojewodztwo.Id
                    };

                    powiatyDict[klucz] = powiat;
                    await _context.Powiaty.AddAsync(powiat);
                }
            }

            await _context.SaveChangesAsync();
            return powiatyDict;
        }
    }
}