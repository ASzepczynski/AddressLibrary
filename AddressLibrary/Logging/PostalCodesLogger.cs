// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

namespace AddressLibrary.Logging
{
    /// <summary>
    /// Logger dla procesu ładowania kodów pocztowych, dziedziczący z GeneralLogger
    /// </summary>
    public class PostalCodesLogger : GeneralLogger
    {
        /// <summary>
        /// Konstruktor domyślny - używa pliku "PostalCodesLoader.txt"
        /// </summary>
        public PostalCodesLogger(string? appDataPath)
            : base(appDataPath, "PostalCodesLoader.txt", "Log ładowania pocztowych")
        {
        }

        /// <summary>
        /// ✅ NOWY: Konstruktor z niestandardową nazwą pliku
        /// </summary>
        /// <param name="appDataPath">Ścieżka do katalogu głównego aplikacji</param>
        /// <param name="logFileName">Nazwa pliku logu (np. "PostalCodesLoader_Fuzzy.txt")</param>
        public PostalCodesLogger(string? appDataPath, string logFileName)
            : base(appDataPath, logFileName, "Log ładowania")
        {
        }
    }
}