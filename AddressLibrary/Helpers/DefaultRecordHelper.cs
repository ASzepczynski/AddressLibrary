using AddressLibrary.Data;
using AddressLibrary.Models;
using AddressLibrary.Logging;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Pomocnik do zarządzania domyślnymi rekordami z Id = -1 w tabelach
    /// </summary>
    public static class DefaultRecordHelper
    {
        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu z Id = -1 w tabeli
        /// </summary>
        /// <typeparam name="T">Typ encji (np. CechaUlicy, TytulStopien)</typeparam>
        /// <param name="context">Kontekst bazy danych</param>
        /// <param name="tableName">Nazwa tabeli w bazie danych</param>
        /// <param name="columns">Kolumny do wstawienia (bez Id)</param>
        /// <param name="values">Wartości do wstawienia (odpowiadające kolumnom)</param>
        /// <param name="logger">Opcjonalny logger do logowania operacji</param>
        /// <returns>True jeśli rekord został dodany, False jeśli już istniał</returns>
        public static async Task<bool> EnsureDefaultRecordAsync<T>(
            AddressDbContext context,
            string tableName,
            string[] columns,
            string[] values,
            GeneralLogger? logger = null) where T : class
        {
            var dbSet = context.Set<T>();
            
            // Sprawdź czy rekord z Id = -1 już istnieje
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException($"Typ {typeof(T).Name} nie ma właściwości Id");
            }

            var exists = await dbSet
                .AsNoTracking()
                .AnyAsync(e => EF.Property<int>(e, "Id") == -1);

            if (exists)
            {
                logger?.LogInfo($"Domyślny rekord z ID = -1 w tabeli {tableName} już istnieje");
                return false;
            }

            logger?.LogInfo($"Dodawanie domyślnego rekordu z ID = -1 do tabeli {tableName}");

            try
            {
                if (columns.Length != values.Length)
                {
                    throw new ArgumentException("Liczba kolumn musi być równa liczbie wartości");
                }

                var columnList = string.Join(", ", columns);
                var valueList = string.Join(", ", values);

                var sql = $@"
                    SET IDENTITY_INSERT {tableName} ON;
                    
                    IF NOT EXISTS (SELECT 1 FROM {tableName} WHERE Id = -1)
                    BEGIN
                        INSERT INTO {tableName} (Id, {columnList}) 
                        VALUES (-1, {valueList});
                    END
                    
                    SET IDENTITY_INSERT {tableName} OFF;
                ";

                await context.Database.ExecuteSqlRawAsync(sql);

                logger?.LogInfo($"✓ Dodano domyślny rekord z ID = -1 do tabeli {tableName}");
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError($"❌ Błąd podczas dodawania domyślnego rekordu do {tableName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla CechaUlicy
        /// </summary>
        public static Task<bool> EnsureCechaUlicyDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<CechaUlicy>(
                context,
                "CechyUlic",
                new[] { "Nazwa", "Skrot" },
                new[] { "'brak'", "''" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla TytulStopien
        /// </summary>
        public static Task<bool> EnsureTytulStopienDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<TytulStopien>(
                context,
                "TytulyStopnie",
                new[] { "Nazwa", "Skrot", "Dopelniacz" },
                new[] { "'brak'", "''", "'braku'" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla TypUlicy
        /// </summary>
        public static Task<bool> EnsureTypUlicyDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<TypUlicy>(
                context,
                "TypyUlic",
                new[] { "Prefiks", "TytulStopienId", "Imie", "Imie2", "Nazwisko", "Nazwisko2", "Pseudonim", "Postfiks" },
                new[] { "''", "-1", "''", "''", "''", "''", "''", "'brak'" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla Wojewodztwo
        /// </summary>
        public static Task<bool> EnsureWojewodztwoDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<Wojewodztwo>(
                context,
                "Wojewodztwa",
                new[] { "Kod", "Nazwa" },
                new[] { "'00'", "'Brak'" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla Powiat
        /// </summary>
        public static Task<bool> EnsurePowiatDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<Powiat>(
                context,
                "Powiaty",
                new[] { "Kod", "Nazwa", "WojewodztwoId" },
                new[] { "'0000'", "'Brak'", "-1" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla Gmina
        /// </summary>
        public static Task<bool> EnsureGminaDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<Gmina>(
                context,
                "Gminy",
                new[] { "Kod", "Nazwa", "PowiatId", "RodzajGminyId" },
                new[] { "'0000000'", "'Brak'", "-1", "-1" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla RodzajGminy
        /// </summary>
        public static Task<bool> EnsureRodzajGminyDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<RodzajGminy>(
                context,
                "RodzajeGmin",
                new[] { "Kod", "Nazwa" },
                new[] { "'0'", "'Brak'" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla RodzajMiasta
        /// </summary>
        public static Task<bool> EnsureRodzajMiastaDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<RodzajMiasta>(
                context,
                "RodzajeMiast",
                new[] { "Kod", "Nazwa" },
                new[] { "'--'", "'Brak'" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla Miasto
        /// </summary>
        public static Task<bool> EnsureMiastoDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<Miasto>(
                context,
                "Miasta",
                new[] { "Kod", "Nazwa", "GminaId", "RodzajMiastaId" },
                new[] { "'0000000'", "'Brak'", "-1", "-1" },
                logger
            );
        }

        /// <summary>
        /// Zapewnia istnienie domyślnego rekordu dla Ulica
        /// </summary>
        public static Task<bool> EnsureUlicaDefaultAsync(AddressDbContext context, GeneralLogger? logger = null)
        {
            return EnsureDefaultRecordAsync<Ulica>(
                context,
                "Ulice",
                new[] { "Symbol", "Dzielnica", "MiastoId", "TypUlicyId","CechaUlicyId" },
                new[] { "'0000000'", "''", "-1", "-1", "-1" },
                logger
            );
        }
    }
}