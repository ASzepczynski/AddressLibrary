// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla procesu ładowania kodów pocztowych, dziedziczący z GeneralLogger
    /// </summary>
    public class PostalCodesLogger : GeneralLogger
    {
        public PostalCodesLogger(string? appDataPath)
            : base(appDataPath, "PostalCodesLoader.txt", "Log ładowania kodów pocztowych")
        {
        }
    }
}