using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using AddressLibrary.Services.AddressSearch;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Cache ulic — s³ownik miastoId ? Lista&lt;UlicaCached&gt; z pre-znormalizowanymi komponentami.
    /// </summary>
    public class UliceCache
    {
        private readonly AddressDbContext _context;
        private Dictionary<int, List<UlicaCached>>? _dict;

        public bool IsInitialized => _dict != null;

        public UliceCache(AddressDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            if (_dict != null)
                return;

            var ulice = await _context.Ulice
                .Include(u => u.Miasto)
                .Include(u => u.CechaUlicy)
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien)
                .Where(u => u.Id != -1)
                .AsNoTracking()
                .ToListAsync();

            var cached = ulice.Select(u => new UlicaCached
            {
                Id         = u.Id,
                MiastoId   = u.MiastoId,
                CechaUlicy = u.CechaUlicy,
                Miasto     = u.Miasto,
                Dzielnica  = u.Dzielnica ?? string.Empty,
                TypUlicyId = u.TypUlicyId,

                Prefiks   = Norm(u.TypUlicy?.Prefiks,   u.TypUlicyId),
                Tytul     = NormTytul(u),
                Imie      = Norm(u.TypUlicy?.Imie,      u.TypUlicyId),
                Imie2     = Norm(u.TypUlicy?.Imie2,     u.TypUlicyId),
                Nazwisko  = Norm(u.TypUlicy?.Nazwisko,  u.TypUlicyId),
                Nazwisko2 = Norm(u.TypUlicy?.Nazwisko2, u.TypUlicyId),
                Pseudonim = Norm(u.TypUlicy?.Pseudonim, u.TypUlicyId),
                Postfiks  = Norm(u.TypUlicy?.Postfiks,  u.TypUlicyId),
            }).ToList();

            _dict = cached
                .GroupBy(u => u.MiastoId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public void Invalidate() => _dict = null;

        public bool TryGet(int miastoId, out List<UlicaCached> ulice)
        {
            ulice = new List<UlicaCached>();
            return _dict != null && _dict.TryGetValue(miastoId, out ulice!);
        }

        public List<(string MiastoNazwa, string UlicaNazwa)> FindGlobally(string streetName)
        {
            var result = new List<(string, string)>();
            if (_dict == null || string.IsNullOrWhiteSpace(streetName)) return result;

            var norm = TextNormalizer.Normalize(streetName);
            foreach (var (_, ulice) in _dict)
                foreach (var u in ulice)
                    if (u.GetFullName().Contains(norm) || (u.Nazwisko?.Contains(norm) == true))
                        result.Add((u.Miasto?.Nazwa ?? "?", u.GetDisplayName()));
            return result;
        }

        // ?? helpers ??????????????????????????????????????????????????????????
        private static string Norm(string? value, int typUlicyId) =>
            typUlicyId == -1 || string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : TextNormalizer.Normalize(value);

        private static string NormTytul(Ulica u)
        {
            if (u.TypUlicyId == -1 || u.TypUlicy == null) return string.Empty;
            if (u.TypUlicy.TytulStopienId == -1 || u.TypUlicy.TytulStopien == null) return string.Empty;
            var s = u.TypUlicy.TytulStopien.Dopelniacz ?? u.TypUlicy.TytulStopien.Skrot ?? string.Empty;
            return string.IsNullOrWhiteSpace(s) ? string.Empty : TextNormalizer.Normalize(s);
        }
    }
}
