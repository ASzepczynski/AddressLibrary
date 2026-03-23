using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using AddressLibrary.Services.Dictionaries.TytulyStopnie;

namespace AddressLibrary.Services.Dictionaries
{
    /// <summary>
    /// Fasada dla TytulyStopnieDictionary - dla kompatybilnoœci wstecznej
    /// </summary>
    public class TytulyStopnieDictionaryService
    {
        private readonly TytulyStopnieDictionary _dictionary;

        public TytulyStopnieDictionaryService(AddressDbContext context)
        {
            _dictionary = new TytulyStopnieDictionary(context);
        }

        public Task<List<TytulStopien>> GetAllAsync() => _dictionary.GetAllAsync();
        
        public Task<Dictionary<string, int>> GetSkrotToIdMappingAsync() => _dictionary.GetSkrotToIdMappingAsync();
        
        public int MapSkrotToId(string? tytul) => _dictionary.MapSkrotToId(tytul);
        
        public Task<int> MapSkrotToIdAsync(string? tytul) => _dictionary.MapSkrotToIdAsync(tytul);
        
        public async Task InitializeTitleManagerAsync()
        {
            if (!TitleManager.IsInitialized)
            {
                var tytuly = await _dictionary.GetAllAsync();
                TitleManager.Initialize(tytuly);
            }
        }
        
        public void ClearCache() => _dictionary.ClearCache();
    }
}