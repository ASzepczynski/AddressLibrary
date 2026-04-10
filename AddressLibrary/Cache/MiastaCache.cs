using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Cache miast — s³ownik znormalizowana_nazwa ? Lista&lt;Miasto&gt;.
    /// </summary>
    public class MiastaCache
    {
        private readonly AddressDbContext _context;
        private Dictionary<string, List<Miasto>>? _dict;

        public bool IsInitialized => _dict != null;

        public MiastaCache(AddressDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            if (_dict != null)
                return;

            var miasta = await _context.Miasta
                .Include(m => m.Gmina)
                    .ThenInclude(g => g.Powiat)
                        .ThenInclude(p => p.Wojewodztwo)
                .Include(m => m.Gmina.RodzajGminy)
                .Where(m => m.Id != -1)
                .AsNoTracking()
                .ToListAsync();

            _dict = miasta
                .GroupBy(m => TextNormalizer.Normalize(m.Nazwa))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public void Invalidate() => _dict = null;

        public bool TryGet(string normalizedName, out List<Miasto> miasta)
        {
            miasta = new List<Miasto>();
            return _dict != null && _dict.TryGetValue(normalizedName, out miasta!);
        }

        public List<Miasto> Find(string cityName)
        {
            var normalized = TextNormalizer.Normalize(cityName);
            return TryGet(normalized, out var lista) ? lista : new List<Miasto>();
        }

        public List<Miasto> GetAll() =>
            _dict?.Values.SelectMany(l => l).ToList() ?? new List<Miasto>();
    }
}
