
namespace AddressLibrary.Helpers
{
    public static class Directories
    {
        /// <summary>
        /// Znajduje ścieżkę do pliku Excel w AddressLibrary/AppData/Dictionaries/
        /// </summary>
        public static string GetExcelFilePath(string excelName)
        {
            var projectRoot = Helpers.Configuration.GetAddressLibraryFilePath();
            // Ścieżka do pliku Excel w AddressLibrary/AppData/Dictionaries/
            var excelPath = Path.Combine(projectRoot, "AppData", "Dictionaries", excelName);

            return excelPath;
        }
    }
}
