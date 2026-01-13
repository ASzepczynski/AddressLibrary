using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AddressLibrary.Data
{
    public class AddressDbContextFactory : IDesignTimeDbContextFactory<AddressDbContext>
    {
        public AddressDbContext CreateDbContext(string[] args)
        {
            // Spróbuj znaleźć appsettings.json w kilku lokalizacjach
            var basePaths = new[]
            {
                // 1. Z katalogu projektu AddressLibrary do TerytLoad
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "TerytLoad"),
                // 2. Bezpośrednio z TerytLoad (jeśli jesteśmy w głównym katalogu rozwiązania)
                Path.Combine(Directory.GetCurrentDirectory(), "TerytLoad"),
                // 3. Jeśli jesteśmy już w TerytLoad
                Directory.GetCurrentDirectory()
            };

            IConfigurationRoot? configuration = null;

            foreach (var basePath in basePaths)
            {
                var settingsPath = Path.Combine(basePath, "appsettings.json");
                if (File.Exists(settingsPath))
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: false)
                        .Build();
                    break;
                }
            }

            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "Nie można znaleźć pliku appsettings.json. " +
                    "Upewnij się, że projekt TerytLoad zawiera plik appsettings.json z connection stringiem 'AddressDatabase'.");
            }

            var connectionString = configuration.GetConnectionString("AddressDatabase")
                ?? throw new InvalidOperationException("Connection string 'AddressDatabase' not found in appsettings.json");

            var optionsBuilder = new DbContextOptionsBuilder<AddressDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AddressDbContext(optionsBuilder.Options);
        }
    }
}