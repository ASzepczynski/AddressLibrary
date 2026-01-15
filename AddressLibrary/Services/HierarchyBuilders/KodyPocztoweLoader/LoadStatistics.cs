// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Services.HierarchyBuilders.KodyPocztoweLoader
{
    /// <summary>
    /// Statystyki procesu ³adowania kodów pocztowych
    /// </summary>
    internal class LoadStatistics
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int SkippedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int MultipleGminFound { get; set; }
        public int CorrectedMiastaCount { get; set; }
        public int CorrectedUliceCount { get; set; }
        public int ProcessedCount { get; set; }

        public string FormatSummary(int totalRecords)
        {
            return $"{Environment.NewLine}=== Podsumowanie ==={Environment.NewLine}" +
                   $"Pomyœlnie za³adowano: {SuccessCount}{Environment.NewLine}" +
                   $"B³êdy (brak ulicy): {ErrorCount - SkippedCount}{Environment.NewLine}" +
                   $"Pominiête (brak miejscowoœci): {SkippedCount}{Environment.NewLine}" +
                   $"Duplikaty pominiête: {DuplicateCount}{Environment.NewLine}" +
                   $"Przypadki wielokrotnych gmin: {MultipleGminFound}{Environment.NewLine}" +
                   $"POPRAWIONE Miejscowoœci: {CorrectedMiastaCount}{Environment.NewLine}" +
                   $"POPRAWIONE Ulice: {CorrectedUliceCount}{Environment.NewLine}" +
                   $"£¹cznie rekordów: {totalRecords}{Environment.NewLine}";
        }
    }
}