// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Helpers;
using AddressLibrary.Logging;
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

        public PostalCodeSearchStrategy(
            AddressSearchCache cache,
            AddressDbContext context
            )
        {
            _cache = cache;
            _context = context;
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

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.KodPocztowyNotFound,
                    Message = AddressSearchStatusInfo.GetMessage(
                        AddressSearchStatus.KodPocztowyNotFound,
                        request.KodPocztowy)
                };
                result.AddDiagnostic($"Szukany kod: {request.KodPocztowy}");
                result.AddDiagnostic("Kod nie istnieje w bazie");
                return result;
            }

            diagnostic?.Log($"✓ Znaleziono {kodPocztowyRecords.Count} rekordów z kodem {request.KodPocztowy}");

            // ✅ KROK 3: Filtruj po mieście (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Miasto))
            {
                var normalizedMiasto = TextNormalizer.Normalize(request.Miasto);
                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => TextNormalizer.Normalize(k.Miasto.Nazwa) == normalizedMiasto)
                    .ToList();

                diagnostic?.Log($"  Filtr po mieście '{request.Miasto}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Brak kodów dla miasta '{request.Miasto}'");

                    var result = new AddressSearchResult
                    {
                        Status = AddressSearchStatus.MiastoNotFound,
                        Message = AddressSearchStatusInfo.GetMessage(
                            AddressSearchStatus.MiastoNotFound,
                            request.Miasto)
                    };
                    result.AddDiagnostic($"Kod: {request.KodPocztowy}");
                    result.AddDiagnostic($"Szukane miasto: {request.Miasto}");
                    result.AddDiagnostic($"Miasto nie pasuje do kodu pocztowego");
                    return result;
                }
            }

            // ✅ KROK 4: Filtruj po ulicy (jeśli podano)
            if (!string.IsNullOrWhiteSpace(request.Ulica))
            {
                var normalizedUlica = TextNormalizer.Normalize(request.Ulica);

                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => k.Ulica != null &&
                               TextNormalizer.Normalize(BuildFullStreetName(k.Ulica)) == normalizedUlica)
                    .ToList();

                diagnostic?.Log($"  Filtr po ulicy '{request.Ulica}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Brak kodów dla ulicy '{request.Ulica}'");

                    var result = new AddressSearchResult
                    {
                        Status = AddressSearchStatus.UlicaNotFound,
                        Message = AddressSearchStatusInfo.GetMessage(
                            AddressSearchStatus.UlicaNotFound,
                            $"({request.Ulica}) w miejscowości ({request.Miasto})")
                    };
                    result.AddDiagnostic($"Kod: {request.KodPocztowy}");
                    result.AddDiagnostic($"Miasto: {request.Miasto}");
                    result.AddDiagnostic($"Szukana ulica: {request.Ulica}");
                    result.AddDiagnostic("Ulica nie pasuje do kodu pocztowego");
                    return result;
                }
            }

            // ✅ KROK 5: Filtruj po numerze domu (jeśli podano)
            if (!string.IsNullOrWhiteSpace(normalizedNumerDomu))
            {
                kodPocztowyRecords = kodPocztowyRecords
                    .Where(k => BuildingNumberValidator.IsNumberInRange(normalizedNumerDomu, k.Numery))
                    .ToList();

                diagnostic?.Log($"  Filtr po numerze domu '{normalizedNumerDomu}': {kodPocztowyRecords.Count} rekordów");

                if (kodPocztowyRecords.Count == 0)
                {
                    diagnostic?.Log($"✗ Numer domu '{normalizedNumerDomu}' nie pasuje do żadnego zakresu");

                    var result = new AddressSearchResult
                    {
                        Status = AddressSearchStatus.KodPocztowyNotFound,
                        Message = $"Nie znaleziono kodu pocztowego dla numeru domu '{request.NumerDomu}'"
                    };
                    result.AddDiagnostic($"Kod: {request.KodPocztowy}");
                    result.AddDiagnostic($"Numer domu: {normalizedNumerDomu}");
                    result.AddDiagnostic("Numer nie pasuje do żadnego zakresu");
                    return result;
                }
            }

            // ✅ KROK 6: Wybierz najlepsze dopasowanie
            if (kodPocztowyRecords.Count == 1)
            {
                var match = kodPocztowyRecords[0];
                diagnostic?.Log($"✓ SUKCES: Znaleziono dokładne dopasowanie");

                var result = new AddressSearchResult
                {
                    Status = AddressSearchStatus.Success,
                    KodPocztowy = match,
                    Miasto = match.Miasto,
                    Ulica = match.Ulica,
                    Message = "Znaleziono kod pocztowy"
                };
                result.AddDiagnostic($"Kod: {match.Kod}");
                result.AddDiagnostic($"Miasto: {match.Miasto.Nazwa}");
                if (match.Ulica != null)
                    result.AddDiagnostic($"Ulica: {BuildFullStreetName(match.Ulica)}");
                return result;
            }

            // Wiele dopasowań
            diagnostic?.Log($"⚠ Znaleziono {kodPocztowyRecords.Count} dopasowań");

            var multiResult = new AddressSearchResult
            {
                Status = AddressSearchStatus.MultipleMatches,
                KodPocztowy = kodPocztowyRecords[0],
                Miasto = kodPocztowyRecords[0].Miasto,
                Ulica = kodPocztowyRecords[0].Ulica,
                Message = $"Znaleziono {kodPocztowyRecords.Count} kodów pocztowych pasujących do kryteriów"
            };
            multiResult.AddDiagnostic($"Liczba dopasowań: {kodPocztowyRecords.Count}");
            multiResult.AddDiagnostic($"Kod: {request.KodPocztowy}");

            foreach (var rec in kodPocztowyRecords.Take(5))
            {
                var streetInfo = rec.Ulica != null ? BuildFullStreetName(rec.Ulica) : "brak ulicy";
                multiResult.AddDiagnostic($"  • {rec.Miasto.Nazwa}, {streetInfo}");
            }

            if (kodPocztowyRecords.Count > 5)
                multiResult.AddDiagnostic($"  ... i {kodPocztowyRecords.Count - 5} więcej");

            return multiResult;
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
            var normalizedNazwa2 = UliceUtils.NormalizeOrdinalNumber(ulica.Nazwa2);
            return $"{normalizedNazwa2} {ulica.Nazwa1}".Trim();
        }
    }
}