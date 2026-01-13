using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using Microsoft.Data.SqlClient;
using CsvHelper.Configuration;
using AddressLibrary.Models;

public static class SqlInserter
{
    public static void InsertIntoSqlServer(string csvPath, string connectionString, string database)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        // ensure database exists
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"IF DB_ID('{database}') IS NULL CREATE DATABASE [{database}];";
            cmd.ExecuteNonQuery();
        }

        conn.ChangeDatabase(database);

        // create table CPna if not exists
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"IF OBJECT_ID('dbo.CPna') IS NULL
BEGIN
    CREATE TABLE dbo.CPna(
        Id INT IDENTITY PRIMARY KEY,
        Kod NVARCHAR(50),
        Miasto NVARCHAR(200),
        Dzielnica NVARCHAR(200),
        Ulica NVARCHAR(400),
        Gmina NVARCHAR(200),
        Powiat NVARCHAR(200),
        Wojewodztwo NVARCHAR(200),
        Numery NVARCHAR(400)
    )
END";
            cmd.ExecuteNonQuery();
        }

        // detect header presence and delimiter
        var encoding = Encoding.GetEncoding(1250);
        string firstLine = File.ReadLines(csvPath, encoding).FirstOrDefault() ?? string.Empty;
        char delimiter = firstLine.Count(c => c == ';') >= firstLine.Count(c => c == ',') ? ';' : ',';

        string[] expectedHeaders = new[] { "kod", "miasto", "dzielnica", "ulica", "gmina", "powiat", "wojewodztwo", "numery" };
        // try to detect if first line contains any expected header names
        var headerTokens = firstLine.Split(delimiter).Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToArray();
        bool hasHeader = headerTokens.Any(t => expectedHeaders.Contains(t));

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = hasHeader,
            Delimiter = delimiter.ToString(),
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = h => ((h.Header ?? string.Empty).Trim('"').Trim().ToLowerInvariant())
        };

        // read CSV and insert rows
        using var reader = new StreamReader(csvPath, encoding);
        using var csv = new CsvReader(reader, csvConfig);

        if (hasHeader)
        {
            csv.Context.RegisterClassMap<CPnaIndexMap>();
            // ensure header is read and mapped
            csv.Read();
            csv.ReadHeader();
        }
        else
        {
            csv.Context.RegisterClassMap<CPnaIndexMap>();
        }

        var recordsCsv = csv.GetRecords<Pna>();

        using var tran = conn.BeginTransaction();
        using var insertCmd = conn.CreateCommand();
        insertCmd.Transaction = tran;
        insertCmd.CommandText = @"INSERT INTO dbo.CPna (Kod, Miasto, Dzielnica, Ulica, Gmina, Powiat, Wojewodztwo, Numery)
VALUES (@Kod, @Miasto, @Dzielnica, @Ulica, @Gmina, @Powiat, @Wojewodztwo, @Numery)";

        insertCmd.Parameters.Add(new SqlParameter("@Kod", System.Data.SqlDbType.NVarChar, 50));
        insertCmd.Parameters.Add(new SqlParameter("@Miasto", System.Data.SqlDbType.NVarChar, 200));
        insertCmd.Parameters.Add(new SqlParameter("@Dzielnica", System.Data.SqlDbType.NVarChar, 200));
        insertCmd.Parameters.Add(new SqlParameter("@Ulica", System.Data.SqlDbType.NVarChar, 400));
        insertCmd.Parameters.Add(new SqlParameter("@Gmina", System.Data.SqlDbType.NVarChar, 200));
        insertCmd.Parameters.Add(new SqlParameter("@Powiat", System.Data.SqlDbType.NVarChar, 200));
        insertCmd.Parameters.Add(new SqlParameter("@Wojewodztwo", System.Data.SqlDbType.NVarChar, 200));
        insertCmd.Parameters.Add(new SqlParameter("@Numery", System.Data.SqlDbType.NVarChar, 400));

        foreach (var r in recordsCsv)
        {
            insertCmd.Parameters["@Kod"].Value = (object)r.Kod ?? DBNull.Value;
            insertCmd.Parameters["@Miasto"].Value = (object)r.Miasto ?? DBNull.Value;
            insertCmd.Parameters["@Dzielnica"].Value = (object)r.Dzielnica ?? DBNull.Value;
            insertCmd.Parameters["@Ulica"].Value = (object)r.Ulica ?? DBNull.Value;
            insertCmd.Parameters["@Gmina"].Value = (object)r.Gmina ?? DBNull.Value;
            insertCmd.Parameters["@Powiat"].Value = (object)r.Powiat ?? DBNull.Value;
            insertCmd.Parameters["@Wojewodztwo"].Value = (object)r.Wojewodztwo ?? DBNull.Value;
            insertCmd.Parameters["@Numery"].Value = (object)r.Numery ?? DBNull.Value;

            insertCmd.ExecuteNonQuery();
        }

        tran.Commit();

        Console.WriteLine($"Inserted records into SQL Server database {database}.dbo.CPna");
    }
}

// index-based map for CSVs without headers
public sealed class CPnaIndexMap : ClassMap<Pna>
{
    public CPnaIndexMap()
    {
        Map(m => m.Kod).Index(0);
        Map(m => m.Miasto).Index(1);
        Map(m => m.Dzielnica).Index(2);
        Map(m => m.Ulica).Index(3);
        Map(m => m.Gmina).Index(4);
        Map(m => m.Powiat).Index(5);
        Map(m => m.Wojewodztwo).Index(6);
        Map(m => m.Numery).Index(7);
    }
}