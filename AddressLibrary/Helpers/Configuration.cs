using AddressLibrary.Services.Dictionaries.CechyUlic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressLibrary.Helpers
{
    internal static class Configuration
    {
        public static string GetAddressLibraryFilePath()
        {
            //
            // Nie umiemy wraz z Copilotem wymyślić jak to zrobić dynamicznie
            //
            return  @"c:\src\AddressLibrary\AddressLibrary";
            
        
        }

    }
}
