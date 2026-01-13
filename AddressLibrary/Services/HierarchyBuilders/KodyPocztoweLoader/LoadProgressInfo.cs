// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Informacje o postêpie ³adowania kodów pocztowych
    /// </summary>
    public class LoadProgressInfo
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double PercentageComplete => TotalCount > 0 ? (ProcessedCount * 100.0 / TotalCount) : 0;
        public string CurrentOperation { get; set; } = string.Empty;
    }
}