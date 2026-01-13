// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace AddressLibrary.Services
{
    /// <summary>
    /// Serwis do wyszukiwania kodów pocztowych na podstawie adresu
    /// </summary>
    public class KodPocztowySearchService
    {
        private readonly AddressDbContext _context;

        public KodPocztowySearchService(AddressDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Parametry wyszukiwania kodów pocztowych
        /// </summary>
        public class SearchParameters
        {
            /// <summary>
            /// Kod pocztowy w formacie XX-XXX (opcjonalny)
            /// </summary>
            public string? Kod { get; set; }

            /// <summary>
            /// Nazwa miejscowoœci (opcjonalna, ale wymagana jeœli nie podano kodu)
            /// </summary>
            public string? Miejscowosc { get; set; }

            /// <summary>
            /// Nazwa ulicy (opcjonalna)
            /// </summary>
            public string? Ulica { get; set; }

            /// <summary>
            /// Numer domu (opcjonalny, u¿ywany do filtrowania zakresów)
            /// </summary>
            public string? NumerDomu { get; set; }

            /// <summary>
            /// Numer mieszkania (opcjonalny, obecnie nieu¿ywany)
            /// </summary>
            public string? NumerMieszkania { get; set; }
        }

        /// <summary>
        /// Uniwersalna wyszukiwarka kodów pocztowych
        /// </summary>
        public async Task<List<KodPocztowyZAdresem>> SzukajAsync(SearchParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            // Walidacja - musi byæ podany kod lub miejscowoœæ
            if (string.IsNullOrWhiteSpace(parameters.Kod) && string.IsNullOrWhiteSpace(parameters.Miejscowosc))
            {
                throw new ArgumentException("Nale¿y podaæ kod pocztowy lub nazwê miejscowoœci");
            }

            IQueryable<KodPocztowy> query = _context.KodyPocztowe
                .Include(k => k.Miejscowosc)
                    .ThenInclude(m => m.Gmina)
                        .ThenInclude(g => g.Powiat)
                            .ThenInclude(p => p.Wojewodztwo)
                .Include(k => k.Ulica);

            // Filtruj po kodzie pocztowym
            if (!string.IsNullOrWhiteSpace(parameters.Kod))
            {
                var kodNormalized = NormalizujKodPocztowy(parameters.Kod);
                query = query.Where(k => k.Kod == kodNormalized);
            }

            // Wykonaj zapytanie do bazy
            var results = await query.ToListAsync();

            // Filtruj po miejscowoœci (w pamiêci z normalizacj¹)
            if (!string.IsNullOrWhiteSpace(parameters.Miejscowosc))
            {
                var miejscowoscNorm = NormalizeText(parameters.Miejscowosc);
                results = results
                    .Where(k => k.Miejscowosc != null && 
                               NormalizeText(k.Miejscowosc.Nazwa) == miejscowoscNorm)
                    .ToList();
            }

            // Filtruj po ulicy (w pamiêci z obs³ug¹ Nazwa1+Nazwa2)
            if (!string.IsNullOrWhiteSpace(parameters.Ulica))
            {
                var ulicaNorm = NormalizeText(parameters.Ulica);
                results = results
                    .Where(k => k.Ulica != null && IsStreetMatch(k.Ulica.Nazwa1, k.Ulica.Nazwa2, ulicaNorm))
                    .ToList();
            }
            else
            {
                // Jeœli nie podano ulicy, szukaj rekordów bez ulicy (UlicaId == -1 lub null)
                results = results.Where(k => k.UlicaId == -1 || k.UlicaId == null).ToList();
            }

            // Filtruj po numerze domu (jeœli podano)
            if (!string.IsNullOrWhiteSpace(parameters.NumerDomu))
            {
                results = results
                    .Where(k => CzyNumerPasujeDoZakresu(parameters.NumerDomu, k.Numery))
                    .ToList();
            }

            // Konwertuj na wyniki z pe³nym adresem
            return results.Select(k => new KodPocztowyZAdresem
            {
                KodPocztowy = k,
                Miejscowosc = k.Miejscowosc,
                Ulica = k.Ulica
            }).ToList();
        }

        /// <summary>
        /// Sprawdza czy ulica pasuje, uwzglêdniaj¹c Nazwa1, Nazwa2 i ich kombinacje
        /// </summary>
        private bool IsStreetMatch(string nazwa1, string? nazwa2, string searchTerm)
        {
            // SprawdŸ Nazwa1
            if (IsStreetNameMatch(nazwa1, searchTerm))
                return true;

            // SprawdŸ Nazwa2
            if (!string.IsNullOrEmpty(nazwa2) && IsStreetNameMatch(nazwa2, searchTerm))
                return true;

            // SprawdŸ kombinacjê Nazwa2 + Nazwa1 (np. "Królowej Jadwigi")
            if (!string.IsNullOrEmpty(nazwa2))
            {
                var combined = NormalizeText($"{nazwa2} {nazwa1}");
                if (combined == searchTerm)
                    return true;
            }

            // SprawdŸ kombinacjê Nazwa1 + Nazwa2 (np. "Jadwigi Królowej")
            if (!string.IsNullOrEmpty(nazwa2))
            {
                var combinedReverse = NormalizeText($"{nazwa1} {nazwa2}");
                if (combinedReverse == searchTerm)
                    return true;
            }

            return false;
        }

        private bool IsStreetNameMatch(string streetNameInDb, string searchTerm)
        {
            var normalized = NormalizeText(streetNameInDb);
            
            if (normalized == searchTerm)
                return true;

            var words = normalized.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Contains(searchTerm);
        }

        /// <summary>
        /// Normalizuje tekst: usuwa akcenty, ma³e litery, usuwa przedrostki ulic
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Usuñ akcenty i normalizuj
            var normalized = RemoveDiacritics(text.Trim());

            // Zamieñ na ma³e litery
            normalized = normalized.ToLowerInvariant();

            // Usuñ przedrostki ulic
            normalized = RemoveStreetPrefixes(normalized);

            // Usuñ zbêdne bia³e znaki
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        private static readonly string[] StreetPrefixes = new[]
        {
            "ul.", "ul", "ulica",
            "al.", "al", "aleja", "alei",
            "pl.", "pl", "plac", "placu",
            "os.", "os", "osiedle", "osiedla",
            "oœ.", "oœ",
            "rondo",
            "skwer", "skweru",
            "park", "parku",
            "bulwar", "bulwaru",
            "droga",
            "szosa",
            "œcie¿ka",
            "pasa¿", "pasa¿u"
        };

        private string RemoveStreetPrefixes(string text)
        {
            var sortedPrefixes = StreetPrefixes.OrderByDescending(p => p.Length);

            foreach (var prefix in sortedPrefixes)
            {
                if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
                {
                    return text.Substring(prefix.Length + 1).Trim();
                }
                
                if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }
            }

            return text;
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Normalizuje kod pocztowy do formatu XX-XXX
        /// </summary>
        private string NormalizujKodPocztowy(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return string.Empty;
            }

            // Usuñ wszystko oprócz cyfr
            var cyfry = new string(kod.Where(char.IsDigit).ToArray());

            if (cyfry.Length != 5)
            {
                return kod; // Zwróæ oryginalny jeœli nieprawid³owy format
            }

            return $"{cyfry.Substring(0, 2)}-{cyfry.Substring(2, 3)}";
        }

        /// <summary>
        /// Wyszukuje kody pocztowe pasuj¹ce do podanego adresu (stara metoda dla kompatybilnoœci)
        /// </summary>
        public async Task<List<KodPocztowy>> SzukajKodowPocztowychAsync(int miejscowoscId, int ulicaId, string numerBudynku)
        {
            List<KodPocztowy> kandydaci;

            if (ulicaId != -1)
            {
                // Szukaj z ulic¹
                kandydaci = await _context.KodyPocztowe
                    .Where(k => k.MiejscowoscId == miejscowoscId && k.UlicaId == ulicaId)
                    .ToListAsync();
            }
            else
            {
                // ZMIENIONO: Szukaj z UlicaId == -1 zamiast null
                kandydaci = await _context.KodyPocztowe
                    .Where(k => k.MiejscowoscId == miejscowoscId && k.UlicaId == -1)
                    .ToListAsync();
            }

            // Filtruj po numerze budynku
            var wyniki = new List<KodPocztowy>();

            foreach (var kod in kandydaci)
            {
                if (CzyNumerPasujeDoZakresu(numerBudynku, kod.Numery))
                {
                    wyniki.Add(kod);
                }
            }

            return wyniki;
        }

        /// <summary>
        /// Sprawdza czy numer budynku pasuje do definicji zakresu
        /// </summary>
        private bool CzyNumerPasujeDoZakresu(string numerBudynku, string definicjaZakresow)
        {
            if (string.IsNullOrWhiteSpace(definicjaZakresow))
            {
                return true;
            }

            if (!WyodrebnijNumerCzesc(numerBudynku, out int numer))
            {
                return false;
            }

            var zakresy = definicjaZakresow.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var zakres in zakresy)
            {
                if (CzyNumerPasujeDoPojZakresu(numer, zakres))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CzyNumerPasujeDoPojZakresu(int numer, string zakres)
        {
            zakres = zakres.Trim();

            bool tylkoNieparzyste = zakres.EndsWith("(n)", StringComparison.OrdinalIgnoreCase);
            bool tylkoParzyste = zakres.EndsWith("(p)", StringComparison.OrdinalIgnoreCase);

            if (tylkoNieparzyste || tylkoParzyste)
            {
                zakres = zakres.Substring(0, zakres.Length - 3).Trim();
            }

            bool czyParzysty = numer % 2 == 0;
            if (tylkoNieparzyste && czyParzysty)
            {
                return false;
            }
            if (tylkoParzyste && !czyParzysty)
            {
                return false;
            }

            if (zakres.Contains('-'))
            {
                var czesci = zakres.Split('-', StringSplitOptions.TrimEntries);
                if (czesci.Length != 2)
                {
                    return false;
                }

                var poczatek = czesci[0];
                var koniec = czesci[1];

                if (!int.TryParse(poczatek, out int numerPoczatek))
                {
                    return false;
                }

                if (koniec.Equals("DK", StringComparison.OrdinalIgnoreCase))
                {
                    return numer >= numerPoczatek;
                }

                if (!int.TryParse(koniec, out int numerKoniec))
                {
                    return false;
                }

                return numer >= numerPoczatek && numer <= numerKoniec;
            }

            if (int.TryParse(zakres, out int pojedynczyNumer))
            {
                return numer == pojedynczyNumer;
            }

            return false;
        }

        private bool WyodrebnijNumerCzesc(string numerBudynku, out int numer)
        {
            numer = 0;

            if (string.IsNullOrWhiteSpace(numerBudynku))
            {
                return false;
            }

            numerBudynku = numerBudynku.Trim();

            if (numerBudynku.Contains('/'))
            {
                numerBudynku = numerBudynku.Split('/')[0].Trim();
            }

            var cyfry = new StringBuilder();
            foreach (char c in numerBudynku)
            {
                if (char.IsDigit(c))
                {
                    cyfry.Append(c);
                }
                else
                {
                    break;
                }
            }

            if (cyfry.Length > 0)
            {
                return int.TryParse(cyfry.ToString(), out numer);
            }

            return false;
        }

        /// <summary>
        /// Wyszukuje kody pocztowe z za³adowanymi relacjami
        /// </summary>
        public async Task<List<KodPocztowyZAdresem>> SzukajKodowZAdresemAsync(int miejscowoscId, int ulicaId, string numerBudynku)
        {
            var kody = await SzukajKodowPocztowychAsync(miejscowoscId, ulicaId, numerBudynku);

            var wyniki = new List<KodPocztowyZAdresem>();

            foreach (var kod in kody)
            {
                var miejscowosc = await _context.Miejscowosci
                    .Include(m => m.Gmina)
                        .ThenInclude(g => g.Powiat)
                            .ThenInclude(p => p.Wojewodztwo)
                    .FirstOrDefaultAsync(m => m.Id == kod.MiejscowoscId);

                Ulica? ulica = null;
                if (kod.UlicaId.HasValue && kod.UlicaId.Value != -1)
                {
                    ulica = await _context.Ulice.FirstOrDefaultAsync(u => u.Id == kod.UlicaId.Value);
                }

                wyniki.Add(new KodPocztowyZAdresem
                {
                    KodPocztowy = kod,
                    Miejscowosc = miejscowosc,
                    Ulica = ulica
                });
            }

            return wyniki;
        }
    }
}