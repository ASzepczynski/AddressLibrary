using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AddressLibrary.Services
{
    public class CsvDataLoader
    {
        private readonly DbContext _context;

        public CsvDataLoader(DbContext context)
        {
            _context = context;
        }

        public async Task LoadDataFromCsvAsync<T>(string csvFilePath, IProgress<LoadProgress>? progress = null) where T : class
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
                const int batchSize = 1000;
                var dbSet = _context.Set<T>();
                int inserted = 0;

                for (int i = 0; i < records.Count; i += batchSize)
                {
                    var batch = records.Skip(i).Take(batchSize).ToList();
                    await dbSet.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    _context.ChangeTracker.Clear();
                    inserted += batch.Count;

                    progress?.Report(new LoadProgress
                    {
                        CurrentOperation = $"Wstawiono {inserted}/{records.Count} rekordów...",
                        ProcessedCount = inserted,
                        TotalCount = records.Count
                    });
                }
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