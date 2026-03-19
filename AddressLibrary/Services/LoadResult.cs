namespace AddressLibrary.Services
{
    /// <summary>
    /// Wynik ładowania danych
    /// </summary>
    public class LoadResult
    {
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Postęp ładowania danych
    /// </summary>
    public class LoadProgress
    {
        public string CurrentOperation { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public bool IsCompleted { get; set; }
    }
}