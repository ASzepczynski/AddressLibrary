using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class RodzajeGminLoader
    {
        private readonly AddressDbContext _context;

        public RodzajeGminLoader(AddressDbContext context)
        {
            _context = context;
        }

        public async Task LoadAsync()
        {
            // SprawdŸ czy tabela ju¿ zawiera PRAWDZIWE dane (nie tylko rekord -1)
            var existingRealCount = await _context.RodzajeGmin.CountAsync(r => r.Id != -1);
            if (existingRealCount > 0)
            {
                // Prawdziwe dane ju¿ istniej¹, nie dodawaj ponownie
                return;
            }

            // USUNIÊTO: DefaultRecordSeeder.SeedRodzajeGminAsync - to jest robione w BuildHierarchicalStructureAsync

            // Dodaj wszystkie rodzaje gmin zgodnie z TERYT
            var rodzajeGmin = new List<RodzajGminy>
            {
                new RodzajGminy { Kod = "1", Nazwa = "Gmina miejska" },
                new RodzajGminy { Kod = "2", Nazwa = "Gmina wiejska" },
                new RodzajGminy { Kod = "3", Nazwa = "Gmina miejsko-wiejska" },
                new RodzajGminy { Kod = "4", Nazwa = "Miasto w gminie miejsko-wiejskiej" },
                new RodzajGminy { Kod = "5", Nazwa = "Obszar wiejski w gminie miejsko-wiejskiej" },
                new RodzajGminy { Kod = "8", Nazwa = "Dzielnica m. st. Warszawy" },
                new RodzajGminy { Kod = "9", Nazwa = "Delegatura w mieœcie" }
            };

            await _context.RodzajeGmin.AddRangeAsync(rodzajeGmin);
            await _context.SaveChangesAsync();
        }
    }
}