using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

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

        /// <summary>
        /// Automatycznie w³¹cza AutoInclude dla wszystkich w³aœciwoœci nawigacyjnych Foreign Key
        /// </summary>
        /// <typeparam name="T">Typ encji</typeparam>
        /// <param name="builder">EntityTypeBuilder dla konfigurowanej encji</param>
        /// <returns>EntityTypeBuilder dla chainowania</returns>
        public static EntityTypeBuilder<T> AutoIncludeForeignKeys<T>(
            this EntityTypeBuilder<T> builder) where T : class
        {
            var entityType = typeof(T);
            
            // Pobierz wszystkie w³aœciwoœci z atrybutem [ForeignKey]
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                // SprawdŸ czy w³aœciwoœæ ma atrybut ForeignKey
                var foreignKeyAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (foreignKeyAttr != null)
                {
                    // foreignKeyAttr.Name zawiera nazwê w³aœciwoœci nawigacyjnej
                    var navigationPropertyName = foreignKeyAttr.Name;
                    
                    // SprawdŸ czy w³aœciwoœæ nawigacyjna istnieje
                    var navigationProperty = entityType.GetProperty(navigationPropertyName);
                    if (navigationProperty != null && 
                        navigationProperty.PropertyType.IsClass && 
                        navigationProperty.PropertyType != typeof(string))
                    {
                        // W³¹cz AutoInclude dla tej nawigacji
                        builder.Navigation(navigationPropertyName).AutoInclude();
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Kombinacja SetAllColumnsCaseSensitive i AutoIncludeForeignKeys
        /// </summary>
        /// <typeparam name="T">Typ encji</typeparam>
        /// <param name="builder">EntityTypeBuilder dla konfigurowanej encji</param>
        /// <param name="collation">Nazwa collation (domyœlnie Polish_CS_AS)</param>
        /// <returns>EntityTypeBuilder dla chainowania</returns>
        public static EntityTypeBuilder<T> ApplyStandardConfiguration<T>(
            this EntityTypeBuilder<T> builder,
            string collation = "Polish_CS_AS") where T : class
        {
            return builder
                .SetAllColumnsCaseSensitive(collation)
                .AutoIncludeForeignKeys();
        }
    }
}