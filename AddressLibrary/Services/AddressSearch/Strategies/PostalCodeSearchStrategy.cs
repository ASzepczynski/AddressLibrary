// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.AddressSearch.Strategies
{
    /// <summary>
    /// Strategia wyszukiwania na podstawie kodu pocztowego
    /// </summary>
    public class PostalCodeSearchStrategy
    {
        private readonly AddressSearchCache _cache;
        private readonly AddressDbContext _context;
        private readonly TextNormalizer _normalizer;

        public PostalCodeSearchStrategy(
            AddressSearchCache cache,
            AddressDbContext context,
            TextNormalizer normalizer)
        {
            _cache = cache;
            _context = context;
            _normalizer = normalizer;
        }

        public async Task<AddressSearchResult> ExecuteAsync(
            AddressSearchRequest request,
            DiagnosticLogger? diagnostic)
        {
            diagnostic?.Log($"=== STRATEGIA: PostalCodeSearchStrategy ===");
            diagnostic?.Log($"Kod pocztowy: {request.KodPocztowy}");
            diagnostic?.Log($"Miasto: {request.Miasto}");
            diagnostic?.Log($"Ulica: {request.Ulica}");
            diagnostic?.Log($"Numer domu: {request.NumerDomu}");
            diagnostic?.Log($"Numer mieszkania: {request.NumerMieszkania}");

            // ✅ KROK 1: Normalizacja numerów budynków/mieszkań (wielkość liter)
            var normalizedNumerDomu = request.NumerDomu?.ToUpperInvariant()?.Trim();
            var normalizedNumerMieszkania = request.NumerMieszkania?.ToUpperInvariant()?.Trim();

            if (normalizedNumerDomu != request.NumerDomu || normalizedNumerMieszkania != request.NumerMieszkania)
            {
                diagnostic?.Log($"  ✓ Znormalizowano numery:");
                diagnostic?.Log($"    Numer domu: '{request.NumerDomu}' → '{normalizedNumerDomu}'");
                diagnostic?.Log($"    Numer mieszkania: '{request.NumerMieszkania}' → '{normalizedNumerMieszkania}'");
            }

            // ✅ KROK 2: Pobierz kody pocztowe z bazy
            var kodPocztowyRecords = await _context.KodyPocztowe
                .Include(k => k.Miasto)
                    .ThenInclude(m => m.Gmina)
                        .ThenInclude(g => g.Powiat)
                            .ThenInclude(p => p.Wojewodztwo)
                .Include(k => k.Miasto.Gmina.RodzajGminy)
                .Include(k => k.Ulica)
                .Where(k => k.Kod == request.KodPocztowy)
                .ToListAsync();

            if (kodPocztowyRecords.Count == 0)
            {
                diagnostic?.Log($"✗ Nie znaleziono kodu pocztowego '{request.KodPocztowy}' w bazie");
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Message = AddressSearchStatusInfo.GetMessage(
                        AddressSearchStatus.KodPocztowyNotFound,
                        request.KodPocztowy),
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            diagnostic?.Log($"✓ Znaleziono {kodPocztowyRecords.Count} rekordów z kodem {request.KodPocztowy}");

            // ✅ KROK 3: Filtruj po mieście (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Miasto))
            {
                var normalizedMiasto = _normalizer.Normalize(request.Miasto);
                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => _normalizer.Normalize(k.Miasto.Nazwa) == normalizedMiasto)
                    .ToList();

                diagnostic?.Log($"  Filtr po mieście '{request.Miasto}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Brak kodów dla miasta '{request.Miasto}'");
                    return new AddressSearchResult
                    {
                        Status = AddressSearchStatus.MiastoNotFound,
                        Message = AddressSearchStatusInfo.GetMessage(
                            AddressSearchStatus.MiastoNotFound,
                            request.Miasto),
                        DiagnosticInfo = diagnostic?.GetLog()
                    };
                }
            }

            // ✅ KROK 4: Filtruj po ulicy (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                var normalizedUlica = _normalizer.Normalize(request.Ulica);
                
                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => k.Ulica != null && 
                               _normalizer.Normalize(BuildFullStreetName(k.Ulica)) == normalizedUlica)
                    .ToList();

                diagnostic?.Log($"  Filtr po ulicy '{request.Ulica}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Brak kodów dla ulicy '{request.Ulica}'");
                    return new AddressSearchResult
                    {
                        Status = AddressSearchStatus.UlicaNotFound,
                        Message = AddressSearchStatusInfo.GetMessage(
                            AddressSearchStatus.UlicaNotFound,
                            $"{request.Ulica} w miejscowości {request.Miasto}"),
                        DiagnosticInfo = diagnostic?.GetLog()
                    };
                }
            }

            // ✅ KROK 5: Filtruj po numerze domu (jeśli podano)
            if (!string.IsNullOrWhiteSpace(normalizedNumerDomu))
            {
                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => IsNumberInRange(normalizedNumerDomu, k.Numery))
                    .ToList();

                diagnostic?.Log($"  Filtr po numerze domu '{normalizedNumerDomu}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Numer domu '{normalizedNumerDomu}' nie pasuje do żadnego zakresu");
                    return new AddressSearchResult
                    {
                        Status = AddressSearchStatus.KodPocztowyNotFound,
                        Message = $"Nie znaleziono kodu pocztowego dla numeru domu '{request.NumerDomu}'",
                        DiagnosticInfo = diagnostic?.GetLog()
                    };
                }
            }

            // ✅ KROK 6: Wybierz najlepsze dopasowanie
            if (kodPocztowyRecords.Count == 1)
            {
                var match = kodPocztowyRecords[0];
                diagnostic?.Log($"✓ SUKCES: Znaleziono dokładne dopasowanie");
                
                return new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = match,
                    Miasto = match.Miasto,
                    Ulica = match.Ulica,
                    Message = "Znaleziono kod pocztowy",
                    DiagnosticInfo = diagnostic?.GetLog()
                };
            }

            // Wiele dopasowań
            diagnostic?.Log($"⚠ Znaleziono {kodPocztowyRecords.Count} dopasowań");
            
            return new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                KodPocztowy = kodPocztowyRecords[0], // Pierwszy z dopasowań
                Miasto = kodPocztowyRecords[0].Miasto,
                Ulica = kodPocztowyRecords[0].Ulica,
                Message = $"Znaleziono {kodPocztowyRecords.Count} kodów pocztowych pasujących do kryteriów",
                DiagnosticInfo = diagnostic?.GetLog()
            };
        }

        /// <summary>
        /// ✅ Buduje pełną nazwę ulicy (Nazwa2 + Nazwa1)
        /// </summary>
        private string BuildFullStreetName(Ulica ulica)
        {
            if (string.IsNullOrEmpty(ulica.Nazwa2))
            {
                return ulica.Nazwa1;
            }

            // ✅ Normalizacja liczebników porządkowych
            var normalizedNazwa2 = NormalizeOrdinalNumber(ulica.Nazwa2);
            return $"{normalizedNazwa2} {ulica.Nazwa1}".Trim();
        }

        /// <summary>
        /// ✅ Normalizuje liczebniki porządkowe w nazwach ulic
        /// "3-go" → "3", "29-go" → "29", "II-go" → "II"
        /// </summary>
        private string NormalizeOrdinalNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"-?(go|tego|cie)$",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Trim();
        }

        /// <summary>
        /// ✅ Sprawdza czy numer domu pasuje do zakresu numerów
        /// Obsługuje formaty: "1-5", "7-9(n)", "2-10(p)", "11-DK"
        /// </summary>
        private bool IsNumberInRange(string numerDomu, string? zakres)
        {
            if (string.IsNullOrWhiteSpace(zakres))
            {
                // Brak zakresu = wszystkie numery pasują
                return true;
            }

            // Normalizuj numer (usuń litery, zostaw tylko cyfry)
            if (!int.TryParse(System.Text.RegularExpressions.Regex.Replace(numerDomu, @"[^\d]", ""), out var numer))
            {
                return false;
            }

            // Rozdziel zakres przecinkami (może być kilka zakresów)
            var zakresy = zakres.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var z in zakresy)
            {
                var zakresTrimed = z.Trim();

                // Format: "11-DK" (do końca)
                if (zakresTrimed.EndsWith("-DK", StringComparison.OrdinalIgnoreCase))
                {
                    var start = int.Parse(zakresTrimed.Substring(0, zakresTrimed.IndexOf('-')));
                    if (numer >= start)
                        return true;
                }
                // Format: "1-5", "7-9(n)", "2-10(p)"
                else if (zakresTrimed.Contains('-'))
                {
                    var parts = zakresTrimed.Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out var start) &&
                        int.TryParse(System.Text.RegularExpressions.Regex.Replace(parts[1], @"[^\d]", ""), out var end))
                    {
                        if (numer >= start && numer <= end)
                        {
                            // Sprawdź parzystość/nieparzystość
                            if (zakresTrimed.Contains("(n)", StringComparison.OrdinalIgnoreCase))
                            {
                                if (numer % 2 == 1) // nieparzyste
                                    return true;
                            }
                            else if (zakresTrimed.Contains("(p)", StringComparison.OrdinalIgnoreCase))
                            {
                                if (numer % 2 == 0) // parzyste
                                    return true;
                            }
                            else
                            {
                                // Bez ograniczenia parzystości
                                return true;
                            }
                        }
                    }
                }
                // Format: "5" (pojedynczy numer)
                else if (int.TryParse(zakresTrimed, out var pojedynczy))
                {
                    if (numer == pojedynczy)
                        return true;
                }
            }

            return false;
        }
    }
}