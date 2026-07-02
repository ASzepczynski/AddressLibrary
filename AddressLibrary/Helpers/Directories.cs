
namespace AddressLibrary.Helpers
{
    public static class Directories
    {
        /// <summary>
        /// Znajduje ścieżkę do pliku Excel w AddressLibrary/AppData/Db/
        /// </summary>
        public static string GetExcelFilePath(string excelName)
        {
            var projectRoot = Helpers.Configuration.GetAddressLibraryFilePath();
            // Ścieżka do pliku Excel w AddressLibrary/AppData/Db/
            var excelPath = Path.Combine(projectRoot, "AppData", "Db", excelName);

            return excelPath;
        }
    }
}
