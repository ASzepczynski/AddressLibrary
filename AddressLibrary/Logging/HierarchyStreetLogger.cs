// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger kontrolny dla procesu budowania hierarchii TERYT
    /// </summary>
    public class HierarchyStreetLogger : GeneralLogger
    {
        public HierarchyStreetLogger(string? appDataPath)
            : base(appDataPath, "HierarchyStreetBuilder.txt", "Log kontrolny budowania ulic z TERYT")
        {
        }
    }
}
