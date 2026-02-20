// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Metoda dopasowania w wyszukiwaniu adresu
    /// </summary>
    public enum MatchingMethod
    {
        /// <summary>
        /// Dok³adne dopasowanie (exact match)
        /// </summary>
        Strict,

        /// <summary>
        /// Przybli¿one dopasowanie (fuzzy matching - odleg³oœæ Levenshteina, tokenizacja, itp.)
        /// </summary>
        Fuzzy
    }
}