using AddressLibrary.Data;
using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Cache
{
    /// <summary>
    /// Cache kodów pocztowych.
    /// _byMiasto  — miastoId ? lista unikalnych kodów pocztowych (deduplikacja po Kod+UlicaId).
    /// _byUlica   — ulicaId  ? lista kodów pocztowych przypisanych do ulicy.
    /// </summary>
    public class KodyPocztoweCache
    {
        private readonly AddressDbContext _context;
        private Dictionary<int, List<KodPocztowy>>? _byMiasto;
        private Dictionary<int, List<KodPocztowy>>? _byUlica;

        public bool IsInitialized => _byMiasto != null;

        public KodyPocztoweCache(AddressDbContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync()
        {
            if (_byMiasto != null)
                return;

            var kody = await _context.KodyPocztowe
                .IgnoreAutoIncludes()
                .AsNoTracking()
                .ToListAsync();

            // Dla ka¿dego miasta — unikalne kody pocztowe (string) deduplikowane po Kod.
            // Rekord wybrany: preferujemy ten z UlicaId == -1 (kod ca³ego miasta), inaczej pierwszy.
            _byMiasto = kody
                .GroupBy(k => k.MiastoId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(k => k.Kod)
                          .Select(gg => gg.FirstOrDefault(k => k.UlicaId == -1) ?? gg.First())
                          .ToList());

            // Kody przypisane do konkretnych ulic — unikalne po Kod
            _byUlica = kody
                .Where(k => k.UlicaId != -1)
                .GroupBy(k => k.UlicaId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(k => k.Kod)
                          .Select(gg => gg.First())
                          .ToList());
        }

        public void Invalidate()
        {
            _byMiasto = null;
            _byUlica  = null;
        }

        public bool TryGetByMiasto(int miastoId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            return _byMiasto != null && _byMiasto.TryGetValue(miastoId, out kody!);
        }

        public bool TryGetByUlica(int ulicaId, out List<KodPocztowy> kody)
        {
            kody = new List<KodPocztowy>();
            return _byUlica != null && _byUlica.TryGetValue(ulicaId, out kody!);
        }
    }
}


