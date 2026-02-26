using System.Reflection;
using System.Text;

namespace AddressLibrary.Utils
{
    /// <summary>
    /// Klasa statyczna do generycznego wczytywania plików tekstowych do obiektów
    /// </summary>
    public static class GenericFileLoader
    {
        /// <summary>
        /// Wczytuje plik do listy obiektów typu T, mapując kolumny na właściwości w kolejności od lewej do prawej
        /// </summary>
        /// <typeparam name="T">Typ obiektów docelowych (musi mieć konstruktor bezparametrowy)</typeparam>
        /// <param name="filePath">Ścieżka do pliku</param>
        /// <param name="separator">Separator kolumn (domyślnie '|')</param>
        /// <param name="hasHeader">Czy pierwsza linia to nagłówek (domyślnie true)</param>
        /// <param name="encoding">Kodowanie pliku (domyślnie UTF-8)</param>
        /// <param name="skipEmptyLines">Czy pomijać puste linie (domyślnie true)</param>
        /// <returns>Lista obiektów typu T</returns>
        public static async Task<List<T>> LoadFromFileAsync<T>(
            string filePath,
            char separator = '|',
            bool hasHeader = true,
            Encoding? encoding = null,
            bool skipEmptyLines = true) where T : class, new()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Plik nie istnieje: {filePath}");
            }

            encoding ??= Encoding.UTF8;

            // Próba automatycznej detekcji kodowania (UTF-8 vs Windows-1250)
            var lines = await TryReadLinesAsync(filePath, encoding);

            if (lines.Length == 0)
            {
                throw new InvalidOperationException("Plik jest pusty");
            }

            // Pobierz właściwości typu T, które można ustawić
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

            if (properties.Length == 0)
            {
                throw new InvalidOperationException($"Typ {typeof(T).Name} nie ma publicznych właściwości z możliwością zapisu");
            }

            var result = new List<T>();
            var startIndex = hasHeader ? 1 : 0;

            for (int lineIndex = startIndex; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                if (skipEmptyLines && string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(separator);
                var obj = new T();

                // Mapuj kolumny na właściwości (od lewej do prawej)
                for (int i = 0; i < Math.Min(parts.Length, properties.Length); i++)
                {
                    var property = properties[i];
                    var value = parts[i].Trim();

                    try
                    {
                        SetPropertyValue(obj, property, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Błąd podczas ustawiania właściwości '{property.Name}' w linii {lineIndex + 1}: {ex.Message}",
                            ex);
                    }
                }

                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Wczytuje plik do listy obiektów typu T, mapując kolumny na właściwości według nazw z nagłówka
        /// </summary>
        /// <typeparam name="T">Typ obiektów docelowych (musi mieć konstruktor bezparametrowy)</typeparam>
        /// <param name="filePath">Ścieżka do pliku</param>
        /// <param name="separator">Separator kolumn (domyślnie '|')</param>
        /// <param name="encoding">Kodowanie pliku (domyślnie UTF-8)</param>
        /// <param name="skipEmptyLines">Czy pomijać puste linie (domyślnie true)</param>
        /// <param name="ignoreCase">Czy ignorować wielkość liter w nazwach kolumn (domyślnie true)</param>
        /// <returns>Lista obiektów typu T</returns>
        public static async Task<List<T>> LoadFromFileWithHeaderMappingAsync<T>(
            string filePath,
            char separator = '|',
            Encoding? encoding = null,
            bool skipEmptyLines = true,
            bool ignoreCase = true) where T : class, new()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Plik nie istnieje: {filePath}");
            }

            encoding ??= Encoding.UTF8;
            var lines = await TryReadLinesAsync(filePath, encoding);

            if (lines.Length < 2)
            {
                throw new InvalidOperationException("Plik musi zawierać nagłówek i co najmniej jedną linię danych");
            }

            // Parsuj nagłówek
            var headers = lines[0].Split(separator).Select(h => h.Trim()).ToArray();

            // Pobierz właściwości typu T
            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

            // Mapuj nagłówki na indeksy właściwości
            var propertyMapping = new PropertyInfo?[headers.Length];
            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            for (int i = 0; i < headers.Length; i++)
            {
                propertyMapping[i] = properties.FirstOrDefault(p =>
                    p.Name.Equals(headers[i], comparisonType));
            }

            var result = new List<T>();

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                if (skipEmptyLines && string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(separator);
                var obj = new T();

                for (int i = 0; i < Math.Min(parts.Length, propertyMapping.Length); i++)
                {
                    var property = propertyMapping[i];
                    if (property == null)
                        continue;

                    var value = parts[i].Trim();

                    try
                    {
                        SetPropertyValue(obj, property, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Błąd podczas ustawiania właściwości '{property.Name}' w linii {lineIndex + 1}: {ex.Message}",
                            ex);
                    }
                }

                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Próbuje wczytać plik z automatyczną detekcją kodowania
        /// </summary>
        private static async Task<string[]> TryReadLinesAsync(string filePath, Encoding encoding)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(filePath, encoding);

                // Sprawdź czy są znaki zastępowania (może wskazywać na złe kodowanie)
                if (lines.Any(line => line.Contains('�')))
                {
                    // Spróbuj Windows-1250 (polskie kodowanie)
                    lines = await File.ReadAllLinesAsync(filePath, Encoding.GetEncoding(1250));
                }

                return lines;
            }
            catch
            {
                // Fallback na Windows-1250
                return await File.ReadAllLinesAsync(filePath, Encoding.GetEncoding(1250));
            }
        }

        /// <summary>
        /// Ustawia wartość właściwości z automatyczną konwersją typu
        /// </summary>
        private static void SetPropertyValue(object obj, PropertyInfo property, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Dla nullable types ustaw null, dla innych zostaw domyślną wartość
                if (IsNullableType(property.PropertyType))
                {
                    property.SetValue(obj, null);
                }
                return;
            }

            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            try
            {
                if (underlyingType == typeof(string))
                {
                    property.SetValue(obj, value);
                }
                else if (underlyingType == typeof(int))
                {
                    property.SetValue(obj, int.Parse(value));
                }
                else if (underlyingType == typeof(long))
                {
                    property.SetValue(obj, long.Parse(value));
                }
                else if (underlyingType == typeof(double))
                {
                    property.SetValue(obj, double.Parse(value.Replace(',', '.')));
                }
                else if (underlyingType == typeof(decimal))
                {
                    property.SetValue(obj, decimal.Parse(value.Replace(',', '.')));
                }
                else if (underlyingType == typeof(bool))
                {
                    property.SetValue(obj, ParseBool(value));
                }
                else if (underlyingType == typeof(DateTime))
                {
                    property.SetValue(obj, DateTime.Parse(value));
                }
                else if (underlyingType == typeof(Guid))
                {
                    property.SetValue(obj, Guid.Parse(value));
                }
                else if (underlyingType.IsEnum)
                {
                    property.SetValue(obj, Enum.Parse(underlyingType, value, ignoreCase: true));
                }
                else
                {
                    // Spróbuj konwersji ogólnej
                    var convertedValue = Convert.ChangeType(value, underlyingType);
                    property.SetValue(obj, convertedValue);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Nie można przekonwertować wartości '{value}' na typ {underlyingType.Name}",
                    ex);
            }
        }

        /// <summary>
        /// Parsuje wartość bool z różnych formatów
        /// </summary>
        private static bool ParseBool(string value)
        {
            var normalized = value.ToLowerInvariant();
            return normalized is "true" or "1" or "yes" or "tak" or "t" or "y";
        }

        /// <summary>
        /// Sprawdza czy typ jest nullable
        /// </summary>
        private static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
        }

        /// <summary>
        /// Wersja synchroniczna - wczytuje plik do listy obiektów
        /// </summary>
        public static List<T> LoadFromFile<T>(
            string filePath,
            char separator = '|',
            bool hasHeader = true,
            Encoding? encoding = null,
            bool skipEmptyLines = true) where T : class, new()
        {
            return LoadFromFileAsync<T>(filePath, separator, hasHeader, encoding, skipEmptyLines).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Wersja synchroniczna - wczytuje plik do listy obiektów z mapowaniem według nagłówka
        /// </summary>
        public static List<T> LoadFromFileWithHeaderMapping<T>(
            string filePath,
            char separator = '|',
            Encoding? encoding = null,
            bool skipEmptyLines = true,
            bool ignoreCase = true) where T : class, new()
        {
            return LoadFromFileWithHeaderMappingAsync<T>(filePath, separator, encoding, skipEmptyLines, ignoreCase).GetAwaiter().GetResult();
        }
    }
}