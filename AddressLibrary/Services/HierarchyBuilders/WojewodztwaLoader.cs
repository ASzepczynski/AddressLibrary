using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class WojewodztwaLoader
    {
        private readonly AddressDbContext _context;

        public WojewodztwaLoader(AddressDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, Wojewodztwo>> LoadAsync(List<TerytTerc> tercData)
        {
            var wojewodztwaDict = new Dictionary<string, Wojewodztwo>();

            // USUNIÊTO: seeder ju¿ zosta³ wywo³any w BuildHierarchicalStructureAsync
            
            // Wyci¹gnij unikalne województwa (bez kodu "00" - to jest "Brak")
            var wojewodztwaKody = tercData
                .Where(t => !string.IsNullOrEmpty(t.Wojewodztwo) && t.Wojewodztwo != "00")
                .Select(t => t.Wojewodztwo)
                .Distinct()
                .ToList();

            foreach (var kod in wojewodztwaKody)
            {
                var tercWoj = tercData.FirstOrDefault(t => 
                    t.Wojewodztwo == kod && 
                    t.Powiat == "" && 
                    t.Gmina == "");
                    
                if (tercWoj != null)
                {
                    var wojewodztwo = new Wojewodztwo
                    {
                        Kod = kod,
                        Nazwa = tercWoj.Nazwa
                    };
                    wojewodztwaDict[kod] = wojewodztwo;
                    await _context.Wojewodztwa.AddAsync(wojewodztwo);
                }
            }

            await _context.SaveChangesAsync();
            return wojewodztwaDict;
        }
    }
}