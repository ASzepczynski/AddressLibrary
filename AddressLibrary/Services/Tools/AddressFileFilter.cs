using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AddressLibrary.Services.Tools
{
    /// <summary>
    /// Narzêdzie do filtrowania pliku adresów na podstawie listy b³êdnych identyfikatorów.
    /// </summary>
    public static class AddressFileFilter
    {
        /// <summary>
        /// Filtruje linie z pliku wejœciowego, pozostawiaj¹c tylko te, których pierwsza kolumna wystêpuje w pliku z b³êdami.
        /// </summary>
        /// <param name="inputPath">Œcie¿ka do pliku z adresami (np. adresy.txt)</param>
        /// <param name="errorsPath">Œcie¿ka do pliku z b³êdami (np. adres_bledy.txt)</param>
        /// <param name="outputPath">Œcie¿ka do pliku wynikowego (np. adresy_nowe.txt)</param>
        /// <param name="delimiter">Separator kolumn (domyœlnie tabulator)</param>
        public static void FilterByFirstColumn(
            string inputPath,
            string errorsPath,
            string outputPath,
            char delimiter = '|')
        {
            // Wczytaj identyfikatory z pliku b³êdów
            var errorIds = File.ReadLines(errorsPath)
                .Select(line => line.Split(delimiter)[0].Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet();

            // Przefiltruj linie z pliku adresów
            var filtered = File.ReadLines(inputPath)
                .Where(line =>
                {
                    var firstCol = line.Split(delimiter)[0].Trim();
                    return errorIds.Contains(firstCol);
                });

            File.WriteAllLines(outputPath, filtered);
        }
    }
}