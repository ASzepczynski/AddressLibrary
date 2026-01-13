using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using AddressLibrary.Models;

namespace AddressLibrary.Services
{
    public class CsvDataLoader
    {
        private readonly DbContext _context;

        public CsvDataLoader(DbContext context)
        {
            _context = context;
        }

        public async Task LoadDataFromCsvAsync<T>(string csvFilePath) where T : class
        {
            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"Plik CSV nie zosta³ znaleziony: {csvFilePath}");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true, // Pomija pierwsz¹ liniê (nag³ówki)
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, config);

            // Rejestracja mapy, która pomija pole Id
            csv.Context.RegisterClassMap(CreateMapForType<T>());

            var records = csv.GetRecords<T>().ToList();

            if (records.Any())
            {
                var dbSet = _context.Set<T>();
                await dbSet.AddRangeAsync(records);
                await _context.SaveChangesAsync();
            }
        }

        private ClassMap<T> CreateMapForType<T>() where T : class
        {
            var map = new DefaultClassMap<T>();
            
            var properties = typeof(T).GetProperties()
                .Where(p => p.Name != "Id" && p.CanWrite)
                .ToList();

            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                map.Map(typeof(T), property).Index(i);
            }

            return map;
        }
    }

    public class DefaultClassMap<T> : ClassMap<T> where T : class
    {
    }
}