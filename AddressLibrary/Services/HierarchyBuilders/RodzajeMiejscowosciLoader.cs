using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class RodzajeMiastLoader
    {
        private readonly AddressDbContext _context;

        public RodzajeMiastLoader(AddressDbContext context)
        {
            _context = context;
        }

        public async Task LoadAsync()
        {
            // ZMIANA: Wyczyœæ ChangeTracker przed sprawdzeniem
            _context.ChangeTracker.Clear();

            // SprawdŸ czy tabela ju¿ zawiera PRAWDZIWE dane (nie tylko rekord -1)
            var existingRealCount = await _context.RodzajeMiast.CountAsync(rm => rm.Id != -1);
            if (existingRealCount > 0)
            {
                // Prawdziwe dane ju¿ istniej¹, nie dodawaj ponownie
                return;
            }

            // Za³aduj z TerytWmRodz
            var wmRodzData = await _context.TerytWmRodz.ToListAsync();

            if (!wmRodzData.Any())
            {
                // Brak danych w TerytWmRodz - nie ma czego ³adowaæ
                return;
            }

            // ZMIANA: Grupuj po Kod, aby unikn¹æ duplikatów i filtruj puste kody
            var rodzajeMiasta = wmRodzData
                .Where(wmRodz => !string.IsNullOrWhiteSpace(wmRodz.RozdzajMiasta))
                .GroupBy(wmRodz => wmRodz.RozdzajMiasta)
                .Select(group => new RodzajMiasta
                {
                    Kod = group.Key,
                    Nazwa = group.First().Nazwa
                })
                .ToList();

            if (rodzajeMiasta.Any())
            {
                await _context.RodzajeMiast.AddRangeAsync(rodzajeMiasta);
                await _context.SaveChangesAsync();
                
                // Wyczyœæ ChangeTracker po zapisie
                _context.ChangeTracker.Clear();
            }
        }
    }
}