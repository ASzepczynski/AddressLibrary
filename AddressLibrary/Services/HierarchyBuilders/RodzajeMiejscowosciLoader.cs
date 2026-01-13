using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.HierarchyBuilders
{
    public class RodzajeMiejscowosciLoader
    {
        private readonly AddressDbContext _context;

        public RodzajeMiejscowosciLoader(AddressDbContext context)
        {
            _context = context;
        }

        public async Task LoadAsync()
        {
            // ZMIANA: Wyczyœæ ChangeTracker przed sprawdzeniem
            _context.ChangeTracker.Clear();

            // SprawdŸ czy tabela ju¿ zawiera PRAWDZIWE dane (nie tylko rekord -1)
            var existingRealCount = await _context.RodzajeMiejscowosci.CountAsync(rm => rm.Id != -1);
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
            var rodzajeMiejscowosci = wmRodzData
                .Where(wmRodz => !string.IsNullOrWhiteSpace(wmRodz.RozdzajMiasta))
                .GroupBy(wmRodz => wmRodz.RozdzajMiasta)
                .Select(group => new RodzajMiejscowosci
                {
                    Kod = group.Key,
                    Nazwa = group.First().Nazwa
                })
                .ToList();

            if (rodzajeMiejscowosci.Any())
            {
                await _context.RodzajeMiejscowosci.AddRangeAsync(rodzajeMiejscowosci);
                await _context.SaveChangesAsync();
                
                // Wyczyœæ ChangeTracker po zapisie
                _context.ChangeTracker.Clear();
            }
        }
    }
}