using System.Globalization;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.IO;

namespace AddressLibrary.Helpers
{
    /// <summary>
    /// Zapisuje kolekcjê obiektów typu T do pliku tekstowego.
    /// Nag³ówek tworzony jest na podstawie w³aœciwoœci publicznych typu (refleksja).
    /// Domyœlny separator pól to '|', kodowanie UTF-8.
    /// </summary>
    public static class ExcelTableWriter
    {
        public static void WriteToTextFile<T>(IEnumerable<T> items, string outputFilePath, char separator = '|', Encoding? encoding = null, bool includeHeader = true, IEnumerable<string>? excludeColumns = null)
        {
            encoding ??= Encoding.UTF8;

            var dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var writer = new StreamWriter(outputFilePath, false, encoding);

            // Order properties by metadata token to approximate declaration order in the type
            var excludes = new HashSet<string>(excludeColumns ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !excludes.Contains(p.Name))
                .OrderBy(p => p.MetadataToken)
                .ToArray();

            if (includeHeader)
            {
                var header = string.Join(separator, props.Select(p => p.Name));
                writer.WriteLine(header);
            }

            foreach (var item in items)
            {
                var parts = new List<string>(props.Length);
                foreach (var p in props)
                {
                    var val = p.GetValue(item, null);
                    var text = ConvertValueToString(val);
                    // usuwamy znaki nowych linii i separator aby nie psuæ formatu
                    if (text != null)
                    {
                        text = text.Replace('\r', ' ').Replace('\n', ' ');
                        if (separator != '\0')
                            text = text.Replace(separator.ToString(), " ");
                    }
                    parts.Add(text ?? string.Empty);
                }

                writer.WriteLine(string.Join(separator, parts));
            }
        }

        public static async Task WriteToTextFileAsync<T>(IEnumerable<T> items, string outputFilePath, char separator = '|', Encoding? encoding = null, bool includeHeader = true, IEnumerable<string>? excludeColumns = null)
        {
            encoding ??= Encoding.UTF8;

            var dir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var writer = new StreamWriter(outputFilePath, false, encoding);

            // Order properties by metadata token to approximate declaration order in the type
            var excludes = new HashSet<string>(excludeColumns ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !excludes.Contains(p.Name))
                .OrderBy(p => p.MetadataToken)
                .ToArray();

            if (includeHeader)
            {
                var header = string.Join(separator, props.Select(p => p.Name));
                await writer.WriteLineAsync(header);
            }

            foreach (var item in items)
            {
                var parts = new List<string>(props.Length);
                foreach (var p in props)
                {
                    var val = p.GetValue(item, null);
                    var text = ConvertValueToString(val);
                    if (text != null)
                    {
                        text = text.Replace('\r', ' ').Replace('\n', ' ');
                        if (separator != '\0')
                            text = text.Replace(separator.ToString(), " ");
                    }
                    parts.Add(text ?? string.Empty);
                }

                await writer.WriteLineAsync(string.Join(separator, parts));
            }

            await writer.FlushAsync();
        }

        private static string? ConvertValueToString(object? value)
        {
            if (value == null) return null;

            switch (value)
            {
                case DateTime dt:
                    return dt.ToString("o", CultureInfo.InvariantCulture);
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
