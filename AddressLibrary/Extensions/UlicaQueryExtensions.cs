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
        /// Automatycznie do³¹cza TypUlicy z TytulStopien (wymagane dla Nazwa1/Nazwa2 i Tytul)
        /// </summary>
        public static IQueryable<Ulica> IncludeTypUlicy(this IQueryable<Ulica> query)
        {
            return query
                .Include(u => u.TypUlicy)
                    .ThenInclude(t => t.TytulStopien);
        }

        /// <summary>
        /// Sortuje ulice po Nazwa1, potem Nazwa2 (wymaga wczeœniejszego za³adowania TypUlicy)
        /// UWAGA: Dzia³a tylko w pamiêci (po ToListAsync), nie w SQL
        /// </summary>
        public static List<Ulica> SortByNazwa(this List<Ulica> ulice)
        {
            return ulice
                .OrderBy(u => u.Nazwa1)
                .ThenBy(u => u.Nazwa2)
                .ToList();
        }
    }
}