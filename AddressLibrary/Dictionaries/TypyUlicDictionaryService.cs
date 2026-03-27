using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Dictionaries
{
    /// <summary>
    /// Centralny serwis do ³adowania i cache'owania s³ownika TypyUlic
    /// </summary>
    public class TypyUlicDictionaryService
    {
        private readonly AddressDbContext _context;
        private Dictionary<TypUlicyKey, int>? _typyUlicDict;

        public TypyUlicDictionaryService(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// £aduje s³ownik mapuj¹cy TypUlicyKey -> Id
        /// </summary>
        public async Task<Dictionary<TypUlicyKey, int>> GetTypyUlicMappingAsync()
        {
            if (_typyUlicDict != null)
                return _typyUlicDict;

            _typyUlicDict = await _context.TypyUlic
                .AsNoTracking()
                .ToDictionaryAsync(
                    t => new TypUlicyKey
                    {
                        Prefiks = t.Prefiks ?? "",
                        TytulStopienId = t.TytulStopienId,
                        Imie = t.Imie ?? "",
                        Imie2 = t.Imie2 ?? "",
                        Nazwisko = t.Nazwisko ?? "",
                        Nazwisko2 = t.Nazwisko2 ?? "",
                        Pseudonim = t.Pseudonim ?? "",
                        Postfiks = t.Postfiks ?? ""
                    },
                    t => t.Id,
                    new TypUlicyKeyEqualityComparer()
                );

            return _typyUlicDict;
        }

        /// <summary>
        /// Znajduje Id TypUlicy na podstawie komponentów
        /// </summary>
        public async Task<int?> FindTypUlicyIdAsync(
            string? prefiks,
            int tytulStopienId,
            string? imie,
            string? imie2,
            string? nazwisko,
            string? nazwisko2,
            string? pseudonim,
            string? postfiks)
        {
            var dict = await GetTypyUlicMappingAsync();

            var key = new TypUlicyKey
            {
                Prefiks = prefiks ?? "",
                TytulStopienId = tytulStopienId,
                Imie = imie ?? "",
                Imie2 = imie2 ?? "",
                Nazwisko = nazwisko ?? "",
                Nazwisko2 = nazwisko2 ?? "",
                Pseudonim = pseudonim ?? "",
                Postfiks = postfiks ?? ""
            };

            if (dict.TryGetValue(key, out var id))
                return id;

            return null;
        }

        /// <summary>
        /// Czyœci cache
        /// </summary>
        public void ClearCache()
        {
            _typyUlicDict = null;
        }
    }

    /// <summary>
    /// Klucz do wyszukiwania TypUlicy (wszystkie pola oprócz Id)
    /// </summary>
    public class TypUlicyKey
    {
        public string Prefiks { get; set; } = "";
        public int TytulStopienId { get; set; }
        public string Imie { get; set; } = "";
        public string Imie2 { get; set; } = "";
        public string Nazwisko { get; set; } = "";
        public string Nazwisko2 { get; set; } = "";
        public string Pseudonim { get; set; } = "";
        public string Postfiks { get; set; } = "";
    }

    /// <summary>
    /// Comparer dla TypUlicyKey
    /// </summary>
    public class TypUlicyKeyEqualityComparer : IEqualityComparer<TypUlicyKey>
    {
        public bool Equals(TypUlicyKey? x, TypUlicyKey? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Prefiks == y.Prefiks &&
                   x.TytulStopienId == y.TytulStopienId &&
                   x.Imie == y.Imie &&
                   x.Imie2 == y.Imie2 &&
                   x.Nazwisko == y.Nazwisko &&
                   x.Nazwisko2 == y.Nazwisko2 &&
                   x.Pseudonim == y.Pseudonim &&
                   x.Postfiks == y.Postfiks;
        }

        public int GetHashCode(TypUlicyKey obj)
        {
            return HashCode.Combine(
                obj.Prefiks,
                obj.TytulStopienId,
                obj.Imie,
                obj.Imie2,
                obj.Nazwisko,
                obj.Nazwisko2,
                HashCode.Combine(obj.Pseudonim, obj.Postfiks)
            );
        }
    }
}