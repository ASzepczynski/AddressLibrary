using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Extensions
{
    /// <summary>
    /// Extension methods dla zapytañ na Ulica
    /// </summary>
    public static class UlicaQueryExtensions
    {
        /// <summary>
        /// Automatycznie do³¹cza TypUlicy (wymagane dla Nazwa1/Nazwa2)
        /// </summary>
        public static IQueryable<Ulica> IncludeTypUlicy(this IQueryable<Ulica> query)
        {
            return query.Include(u => u.TypUlicy);
        }

        /// <summary>
        /// Sortuje ulice po Cecha i Nazwa1 (wymaga wczeœniejszego Include)
        /// UWAGA: Dzia³a tylko na List<Ulica> (po ToListAsync), nie na IQueryable
        /// </summary>
        public static List<Ulica> SortByNazwa(this List<Ulica> ulice)
        {
            return ulice
                .OrderBy(u => u.Cecha)
                .ThenBy(u => u.Nazwa1)
                .ToList();
        }
    }
}