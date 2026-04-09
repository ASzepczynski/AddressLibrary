using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Data
{
    /// <summary>
    /// Lekki mechanizm w³asnych migracji schematu bazy danych.
    /// Wykonuje skrypty SQL w kolejnoœci wg nazwy. Ka¿dy skrypt wykonywany jest dok³adnie raz.
    /// Historia wykonanych migracji przechowywana jest w tabeli __SchemaVersion.
    /// </summary>
    public static class SchemaMigrator
    {
        // Skrypty migracji w kolejnoœci wykonania.
        // Klucz: unikalny identyfikator (np. "0001_NazwaZmiany"), Wartoœæ: SQL do wykonania.
        private static readonly IReadOnlyList<(string Id, string Sql)> Migrations =
        [
            (
                "0001_AddZasiegToUrzedySkarbowe",
                """
                ALTER TABLE UrzedySkarbowe
                ADD Zasieg NVARCHAR(200) NOT NULL DEFAULT '';
                """
            ),
        ];

        /// <summary>
        /// Sprawdza i aplikuje brakuj¹ce migracje. Wywo³aj przy starcie aplikacji.
        /// </summary>
        public static async Task ApplyAsync(AddressDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            if (conn is not SqlConnection sqlConn)
                return;

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await EnsureVersionTableAsync(sqlConn);

            var applied = await GetAppliedMigrationsAsync(sqlConn);

            foreach (var (id, sql) in Migrations)
            {
                if (applied.Contains(id))
                    continue;

                await using var tx = sqlConn.BeginTransaction();
                try
                {
                    await using var cmd = new SqlCommand(sql, sqlConn, tx);
                    await cmd.ExecuteNonQueryAsync();

                    await using var insert = new SqlCommand(
                        "INSERT INTO __SchemaVersion (MigrationId, AppliedAt) VALUES (@id, @at)",
                        sqlConn, tx);
                    insert.Parameters.AddWithValue("@id", id);
                    insert.Parameters.AddWithValue("@at", DateTime.UtcNow);
                    await insert.ExecuteNonQueryAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }

        private static async Task EnsureVersionTableAsync(SqlConnection conn)
        {
            const string sql = """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.tables WHERE name = '__SchemaVersion'
                )
                CREATE TABLE __SchemaVersion (
                    MigrationId  NVARCHAR(200) NOT NULL PRIMARY KEY,
                    AppliedAt    DATETIME2     NOT NULL
                );
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<HashSet<string>> GetAppliedMigrationsAsync(SqlConnection conn)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using var cmd = new SqlCommand("SELECT MigrationId FROM __SchemaVersion", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0));

            return result;
        }
    }
}
