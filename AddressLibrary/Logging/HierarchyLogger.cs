// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger kontrolny dla procesu budowania hierarchii TERYT
    /// </summary>
    public class HierarchyLogger : GeneralLogger
    {
        public HierarchyLogger(string? appDataPath)
            : base(appDataPath, "HierarchyBuilder.txt", "Log kontrolny budowania hierarchii TERYT")
        {
        }
    }
}
