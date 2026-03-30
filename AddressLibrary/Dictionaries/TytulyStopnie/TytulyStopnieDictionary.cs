using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Dictionaries.TytulyStopnie
{
    /// <summary>
    /// Centralny s³ownik dla TytulyStopnie - zarz¹dzanie cache i dostêp do danych
    /// </summary>
    public class TytulyStopnieDictionary
    {
        private readonly AddressDbContext _context;
        private Dictionary<string, TytulStopien>? _nazwaDict;
        private Dictionary<string, TytulStopien>? _skrotDict;
        private Dictionary<string, int>? _skrotToIdDict;
        private Dictionary<string, int>? _dopelniaczToIdDict;
        private List<TytulStopien>? _allTytuly;

        public TytulyStopnieDictionary(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Pobiera wszystkie tytu³y z bazy danych (z cache)
        /// </summary>
        public async Task<List<TytulStopien>> GetAllAsync()
        {
            if (_allTytuly == null)
            {
                _allTytuly = await _context.TytulyStopnie
                    .AsNoTracking()
                    .ToListAsync();
            }
            return _allTytuly;
        }

        /// <summary>
        /// Pobiera s³ownik Nazwa -> TytulStopien
        /// </summary>
        public async Task<Dictionary<string, TytulStopien>> GetByNazwaAsync()
        {
            if (_nazwaDict == null)
            {
                var tytuly = await GetAllAsync();
                _nazwaDict = tytuly.ToDictionary(
                    t => t.Nazwa,
                    t => t,
                    StringComparer.OrdinalIgnoreCase
                );
            }
            return _nazwaDict;
        }

        /// <summary>
        /// Pobiera s³ownik Skrot -> TytulStopien (obs³uguje duplikaty - bierze pierwszy)
        /// </summary>
        public async Task<Dictionary<string, TytulStopien>> GetBySkrotAsync()
        {
            if (_skrotDict == null)
            {
                var tytuly = await GetAllAsync();
                
                // Obs³u¿ duplikaty - weŸ pierwszy wpis dla ka¿dego unikalnego Skrot
                _skrotDict = tytuly
                    .GroupBy(t => t.Skrot, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase
                    );
            }
            return _skrotDict;
        }

        /// <summary>
        /// Pobiera s³ownik Skrot -> Id z wariantami (obs³uguje duplikaty)
        /// </summary>
        public async Task<Dictionary<string, int>> GetSkrotToIdMappingAsync()
        {
            if (_skrotToIdDict != null)
                return _skrotToIdDict;

            var tytulyList = await GetAllAsync();

            // Obs³u¿ duplikaty - weŸ pierwszy wpis dla ka¿dego unikalnego Skrot
            var baseDict = tytulyList
                .Where(t => !string.IsNullOrWhiteSpace(t.Skrot))
                .GroupBy(t => t.Skrot.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Id,
                    StringComparer.OrdinalIgnoreCase
                );

            // Dodaj warianty: bez kropek, bez spacji, kombinacje
            var variants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in baseDict)
            {
                var skrot = kvp.Key;
                var id = kvp.Value;

                // Dodaj orygina³
                variants[skrot] = id;

                // Dodaj warianty
                var variantKeys = new[]
                {
                    skrot.Replace(".", ""),                   // bez kropek
                    skrot.Replace(".", "").Replace(" ", ""),  // bez kropek i spacji
                    skrot.Replace(" ", ""),                   // bez spacji
                    skrot.ToLower(),                          // lowercase
                    skrot.Replace(".", "").ToLower(),
                };

                foreach (var variant in variantKeys)
                {
                    if (!string.IsNullOrEmpty(variant) && !variants.ContainsKey(variant))
                    {
                        variants[variant] = id;
                    }
                }
            }

            _skrotToIdDict = variants;
            return _skrotToIdDict;
        }

        /// <summary>
        /// Pobiera s³ownik Dopelniacz -> Id z wariantami (obs³uguje duplikaty)
        /// </summary>
        public async Task<Dictionary<string, int>> GetDopelniaczToIdMappingAsync()
        {
            if (_dopelniaczToIdDict != null)
                return _dopelniaczToIdDict;

            var tytulyList = await GetAllAsync();

            // Obs³u¿ duplikaty - weŸ pierwszy wpis dla ka¿dego unikalnego Dopelniacz
            var baseDict = tytulyList
                .Where(t => !string.IsNullOrWhiteSpace(t.Dopelniacz))
                .GroupBy(t => t.Dopelniacz.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Id,
                    StringComparer.OrdinalIgnoreCase
                );

            // Dodaj warianty
            var variants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in baseDict)
            {
                var dopelniacz = kvp.Key;
                var id = kvp.Value;

                // Dodaj orygina³
                variants[dopelniacz] = id;

                // Dodaj warianty
                var variantKeys = new[]
                {
                    dopelniacz.ToLower(),
                    dopelniacz.Replace(" ", ""),
                    dopelniacz.Replace(" ", "").ToLower(),
                };

                foreach (var variant in variantKeys)
                {
                    if (!string.IsNullOrEmpty(variant) && !variants.ContainsKey(variant))
                    {
                        variants[variant] = id;
                    }
                }
            }

            _dopelniaczToIdDict = variants;
            return _dopelniaczToIdDict;
        }

        /// <summary>
        /// Mapuje skrót tytu³u na Id (wersja synchroniczna, wymaga wczeœniejszego za³adowania)
        /// </summary>
        public int MapSkrotToId(string? tytul)
        {
            if (string.IsNullOrWhiteSpace(tytul))
                return -1;

            if (_skrotToIdDict == null)
                throw new InvalidOperationException("S³ownik nie zosta³ za³adowany. Wywo³aj najpierw GetSkrotToIdMappingAsync()");
            
            if (_skrotToIdDict.TryGetValue(tytul.Trim(), out int id))
                return id;

            // Spróbuj bez kropek
            var bezKropek = tytul.Replace(".", "").Trim();
            if (_skrotToIdDict.TryGetValue(bezKropek, out id))
                return id;

            // Podziel na czêœci dla z³o¿onych tytu³ów
            var parts = tytul.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (_skrotToIdDict.TryGetValue(part.Trim(), out id))
                    return id;
            }

            return -1;
        }

        /// <summary>
        /// Mapuje dope³niacz tytu³u na Id (wersja synchroniczna, wymaga wczeœniejszego za³adowania)
        /// </summary>
        public int MapDopelniaczToId(string? dopelniacz)
        {
            if (string.IsNullOrWhiteSpace(dopelniacz))
                return -1;

            if (_dopelniaczToIdDict == null)
                throw new InvalidOperationException("S³ownik nie zosta³ za³adowany. Wywo³aj najpierw GetDopelniaczToIdMappingAsync()");

            if (_dopelniaczToIdDict.TryGetValue(dopelniacz.Trim(), out int id))
                return id;

            //// Podziel na czêœci dla z³o¿onych tytu³ów
            //var parts = dopelniacz.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            //foreach (var part in parts)
            //{
            //    if (_dopelniaczToIdDict.TryGetValue(part.Trim(), out id))
            //        return id;
            //}

            return -2;
        }

        /// <summary>
        /// Mapuje skrót tytu³u na Id (wersja asynchroniczna)
        /// </summary>
        public async Task<int> MapSkrotToIdAsync(string? tytul)
        {
            await GetSkrotToIdMappingAsync();
            return MapSkrotToId(tytul);
        }

        /// <summary>
        /// Mapuje dope³niacz tytu³u na Id (wersja asynchroniczna)
        /// </summary>
        public async Task<int> MapDopelniaczToIdAsync(string? dopelniacz)
        {
            await GetDopelniaczToIdMappingAsync();
            return MapDopelniaczToId(dopelniacz);
        }

        /// <summary>
        /// Inicjalizuje TitleManager s³ownikiem z bazy danych
        /// </summary>
        public async Task InitializeTitleManagerAsync()
        {
            if (!TitleManager.IsInitialized)
            {
                var tytuly = await GetAllAsync();
                TitleManager.Initialize(tytuly);
            }
        }

        /// <summary>
        /// Czyœci cache
        /// </summary>
        public void ClearCache()
        {
            _nazwaDict = null;
            _skrotDict = null;
            _skrotToIdDict = null;
            _dopelniaczToIdDict = null;
            _allTytuly = null;
        }
    }
}