using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Metody rozszerzaj¹ce dla konfiguracji Entity Framework
    /// </summary>
    public static class EntityConfigurationExtensions
    {
        /// <summary>
        /// Ustawia collation Polish_CS_AS (case-sensitive) dla wszystkich w³aœciwoœci typu string w encji
        /// </summary>
        /// <typeparam name="T">Typ encji</typeparam>
        /// <param name="builder">EntityTypeBuilder dla konfigurowanej encji</param>
        /// <param name="collation">Nazwa collation (domyœlnie Polish_CS_AS)</param>
        /// <returns>EntityTypeBuilder dla chainowania</returns>
        public static EntityTypeBuilder<T> SetAllColumnsCaseSensitive<T>(
            this EntityTypeBuilder<T> builder, 
            string collation = "Polish_CS_AS") where T : class
        {
            foreach (var property in builder.Metadata.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    property.SetCollation(collation);
                }
            }

            return builder;
        }
    }
}