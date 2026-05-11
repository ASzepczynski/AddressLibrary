using System.Text.Json;

namespace AddressLibrary.Helpers
{
    public static class CloneHelper
    {
        /// <summary>
        /// Klonuje obiekt przez serializacjê JSON (g³êbokie kopiowanie publicznych w³aœciwoœci).
        /// Zwraca kopiê typu T. W razie wartoœci null zwraca default(T).
        /// </summary>
        public static T? Klonuj<T>(T? obj)
        {
            if (obj == null) return default;

            var json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
