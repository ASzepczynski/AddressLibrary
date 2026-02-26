// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Data;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AddressLibrary.Services.Validation
{
    /// <summary>
    /// Serwis do walidacji zakresów numerów w kodach pocztowych
    /// </summary>
    public class PostalCodeRangeValidator
    {
        private readonly AddressDbContext _context;
        private readonly string _logDirectory;
        private readonly List<ParityWarning> _parityWarnings = new();

        public PostalCodeRangeValidator(AddressDbContext context, string logDirectory)
        {
            _context = context;
            _logDirectory = logDirectory;
        }

        public async Task<ValidationReport> ValidateAsync()
        {
            var report = new ValidationReport();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // ✅ POPRAWKA: Pobierz Miejsce (dzielnicę) przez Ulica.Miejsce
            var kodyUlice = await _context.KodyPocztowe
                .Include(k => k.Ulica)
                .Include(k => k.Miasto)
                .ThenInclude(m => m.Gmina)
                .ThenInclude(g => g.Powiat)
                .ThenInclude(p => p.Wojewodztwo)
                .Where(k => !string.IsNullOrEmpty(k.Numery))
                .ToListAsync();

            var kodyMiasta = await _context.KodyPocztowe
                .Include(k => k.Miasto)
                .ThenInclude(m => m.Gmina)
                .ThenInclude(g => g.Powiat)
                .ThenInclude(p => p.Wojewodztwo)
                .Where(k => !string.IsNullOrEmpty(k.Numery))
                .ToListAsync();

            // ✅ POPRAWKA: Grupuj według ULICY + MIASTA (nie ma MiejsceId w Ulica!)
            var grupy = kodyUlice
                .GroupBy(k => new
                {
                    k.UlicaId,
                    k.MiastoId,
                    k.Ulica?.Dzielnica // ✅ Poprawne: k.Ulica.Miejsce.Id
                })
                .ToList();

            report.ProcessedStreets = grupy.Count;

            foreach (var grupa in grupy)
            {
                var kody = grupa.ToList();
                if (kody.Count < 2) continue;

                report.TotalConflicts += CheckConflictsInGroup(kody, report, isStreet: true);
            }

            var grupyMiast = kodyMiasta
                .GroupBy(k => k.MiastoId)
                .ToList();

            report.ProcessedCities = grupyMiast.Count;

            foreach (var grupa in grupyMiast)
            {
                var kody = grupa.ToList();
                if (kody.Count < 2) continue;

                report.TotalConflicts += CheckConflictsInGroup(kody, report, isStreet: false);
            }

            stopwatch.Stop();
            report.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;

            await SaveReportAsync(report);
            await SaveWarningsAsync();

            return report;
        }

        private int CheckConflictsInGroup(List<KodPocztowy> kody, ValidationReport report, bool isStreet)
        {
            int conflictCount = 0;

            for (int i = 0; i < kody.Count; i++)
            {
                for (int j = i + 1; j < kody.Count; j++)
                {
                    var conflict = CheckConflict(kody[i], kody[j], isStreet);
                    if (conflict != null)
                    {
                        report.Conflicts.Add(conflict);
                        conflictCount++;
                    }
                }
            }

            return conflictCount;
        }

        private RangeConflict? CheckConflict(KodPocztowy kod1, KodPocztowy kod2, bool isStreet)
        {
            var zakresy1 = ParseRanges(kod1.Kod, kod1.Numery, kod1.Ulica?.Nazwa1, kod1.Miasto?.Nazwa);
            var zakresy2 = ParseRanges(kod2.Kod, kod2.Numery, kod2.Ulica?.Nazwa1, kod2.Miasto?.Nazwa);

            // ✅ POPRAWKA: Zbieramy UNIKALNE zakresy, które się nakładają (bez duplikatów)
            var conflictingRanges1 = new HashSet<string>();
            var conflictingRanges2 = new HashSet<string>();

            foreach (var r1 in zakresy1)
            {
                foreach (var r2 in zakresy2)
                {
                    if (IsOverlapping(r1, r2))
                    {
                        conflictingRanges1.Add(FormatRange(r1));
                        conflictingRanges2.Add(FormatRange(r2));
                    }
                }
            }

            if (conflictingRanges1.Any())
            {
                return new RangeConflict
                {
                    Kod1 = kod1.Kod,
                    Kod2 = kod2.Kod,
                    Ulica = isStreet ? kod1.Ulica?.Nazwa1 : null,
                    Dzielnica = isStreet ? kod1.Ulica?.Dzielnica : null,
                    Miasto = kod1.Miasto.Nazwa,
                    Numery1 = kod1.Numery,
                    Numery2 = kod2.Numery,
                    ConflictingRange1 = string.Join(", ", conflictingRanges1), // ✅ Bez duplikatów
                    ConflictingRange2 = string.Join(", ", conflictingRanges2),
                    IsStreetLevel = isStreet
                };
            }

            return null;
        }

        private bool IsOverlapping(NumberRange r1, NumberRange r2)
        {
            bool rangeOverlap = !(r1.End < r2.Start || r2.End < r1.Start);
            if (!rangeOverlap) return false;

            if (r1.Type == ParityType.All && r2.Type == ParityType.All)
                return true;

            if (r1.Type == ParityType.All || r2.Type == ParityType.All)
                return true;

            if (r1.Type == r2.Type)
                return true;

            return false;
        }

        private List<NumberRange> ParseRanges(string kod, string numery, string? ulica, string? miasto)
        {
            var ranges = new List<NumberRange>();
            var parts = numery.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var range = ParseSingleRange(kod, part, ulica, miasto);
                if (range != null)
                {
                    ranges.Add(range.Value);
                }
            }

            return ranges;
        }

        private NumberRange? ParseSingleRange(string kod, string range, string? ulica, string? miasto)
        {
            range = range.Trim();
            string originalRange = range;

            bool isEven = range.EndsWith("(p)", StringComparison.OrdinalIgnoreCase);
            bool isOdd = range.EndsWith("(n)", StringComparison.OrdinalIgnoreCase);

            if (isEven || isOdd)
            {
                range = range.Substring(0, range.Length - 3).Trim();
            }

            if (range.Contains('-'))
            {
                var parts = range.Split('-', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) return null;

                if (!int.TryParse(parts[0], out int start)) return null;

                int end;
                bool isDK = false;
                if (parts[1].Equals("DK", StringComparison.OrdinalIgnoreCase))
                {
                    end = int.MaxValue;
                    isDK = true;

                    // ✅ NOWA REGUŁA: Automatyczna parzystość dla N-DK (gdzie N != 1)
                    if (!isEven && !isOdd && start != 1)
                    {
                        bool autoParzyste = start % 2 == 0;

                        _parityWarnings.Add(new ParityWarning
                        {
                            Kod = kod,
                            Miasto = miasto,
                            Ulica = ulica,
                            OriginalRange = originalRange,
                            SuggestedRange = $"{start}-DK({(autoParzyste ? "p" : "n")})",
                            Reason = $"Zakres '{originalRange}' (DK) bez oznaczenia - początek {start} jest {(autoParzyste ? "parzysty" : "nieparzysty")}"
                        });

                        isEven = autoParzyste;
                        isOdd = !autoParzyste;
                    }
                    // ✅ Jeśli start == 1, pozostaw ParityType.All (brak ograniczenia parzystości)
                }
                else if (!int.TryParse(parts[1], out end))
                {
                    return null;
                }

                if (!isEven && !isOdd && !isDK && start % 2 == end % 2)
                {
                    bool autoParzyste = start % 2 == 0;

                    _parityWarnings.Add(new ParityWarning
                    {
                        Kod = kod,
                        Miasto = miasto,
                        Ulica = ulica,
                        OriginalRange = originalRange,
                        SuggestedRange = $"{originalRange}({(autoParzyste ? "p" : "n")})",
                        Reason = $"Zakres '{originalRange}' bez oznaczenia (n)/(p) - oba końce ({start}, {end}) są {(autoParzyste ? "parzyste" : "nieparzyste")}"
                    });

                    isEven = autoParzyste;
                    isOdd = !autoParzyste;
                }

                var type = isEven ? ParityType.Even : (isOdd ? ParityType.Odd : ParityType.All);
                return new NumberRange { Start = start, End = end, Type = type };
            }

            if (int.TryParse(range, out int number))
            {
                if (!isEven && !isOdd)
                {
                    bool autoParzyste = number % 2 == 0;

                    _parityWarnings.Add(new ParityWarning
                    {
                        Kod = kod,
                        Miasto = miasto,
                        Ulica = ulica,
                        OriginalRange = originalRange,
                        SuggestedRange = $"{number}({(autoParzyste ? "p" : "n")})",
                        Reason = $"Pojedynczy numer '{number}' bez oznaczenia (n)/(p) - jest {(autoParzyste ? "parzysty" : "nieparzysty")}"
                    });

                    isEven = autoParzyste;
                    isOdd = !autoParzyste;
                }

                var type = isEven ? ParityType.Even : (isOdd ? ParityType.Odd : ParityType.All);
                return new NumberRange { Start = number, End = number, Type = type };
            }

            return null;
        }

        private string FormatRange(NumberRange range)
        {
            string suffix = range.Type switch
            {
                ParityType.Even => "(p)",
                ParityType.Odd => "(n)",
                _ => ""
            };

            if (range.Start == range.End)
            {
                return $"{range.Start}{suffix}";
            }

            if (range.End == int.MaxValue)
            {
                return $"{range.Start}-DK{suffix}";
            }

            return $"{range.Start}-{range.End}{suffix}";
        }

        private async Task SaveReportAsync(ValidationReport report)
        {
            var logPath = Path.Combine(_logDirectory, "VerifyPostalCodes.txt");
            Directory.CreateDirectory(_logDirectory);

            using var writer = new StreamWriter(logPath, false, System.Text.Encoding.UTF8);

            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync("RAPORT WERYFIKACJI ZAKRESÓW NUMERÓW W KODACH POCZTOWYCH");
            await writer.WriteLineAsync($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"Liczba sprawdzonych ulic: {report.ProcessedStreets:N0}");
            await writer.WriteLineAsync($"Liczba sprawdzonych miast (bez ulic): {report.ProcessedCities:N0}");
            await writer.WriteLineAsync($"Znaleziono konfliktów: {report.TotalConflicts:N0}");
            await writer.WriteLineAsync($"Czas wykonania: {report.ElapsedSeconds:F2}s");

            if (_parityWarnings.Any())
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync($"⚠️  Ostrzeżenia o automatycznej parzystości: {_parityWarnings.Count:N0}");
                await writer.WriteLineAsync($"    (szczegóły w pliku: VerifyPostalCodesWarnings.txt)");
            }

            if (report.TotalConflicts == 0)
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("✓ Nie wykryto żadnych konfliktów!");
            }
            else
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("====================================================================================================");
                await writer.WriteLineAsync("WYKRYTE KONFLIKTY:");
                await writer.WriteLineAsync("====================================================================================================");
                await writer.WriteLineAsync();

                var streetConflicts = report.Conflicts
                    .Where(c => c.IsStreetLevel)
                    .GroupBy(c => new { c.Miasto, c.Dzielnica, c.Ulica })
                    .OrderBy(g => g.Key.Miasto)
                    .ThenBy(g => g.Key.Dzielnica)
                    .ThenBy(g => g.Key.Ulica);

                if (streetConflicts.Any())
                {
                    await writer.WriteLineAsync("╔═══════════════════════════════════════════════════════════════════════════════════════════════╗");
                    await writer.WriteLineAsync("║ KONFLIKTY NA POZIOMIE ULIC                                                                    ║");
                    await writer.WriteLineAsync("╚═══════════════════════════════════════════════════════════════════════════════════════════════╝");
                    await writer.WriteLineAsync();

                    foreach (var group in streetConflicts)
                    {
                        await writer.WriteLineAsync($"Miejscowość: {group.Key.Miasto}");

                        if (!string.IsNullOrEmpty(group.Key.Dzielnica))
                        {
                            await writer.WriteLineAsync($"Dzielnica/Osiedle: {group.Key.Dzielnica}");
                        }

                        await writer.WriteLineAsync($"Ulica: {group.Key.Ulica}");
                        await writer.WriteLineAsync();

                        foreach (var conflict in group)
                        {
                            await writer.WriteLineAsync("  ⚠️ KONFLIKT:");
                            await writer.WriteLineAsync($"     Kod 1: {conflict.Kod1} → zakresy: {conflict.Numery1}");
                            await writer.WriteLineAsync($"     Kod 2: {conflict.Kod2} → zakresy: {conflict.Numery2}");
                            await writer.WriteLineAsync("     Nakładające się zakresy:");
                            await writer.WriteLineAsync($"       • {conflict.ConflictingRange1}");
                            await writer.WriteLineAsync($"       • {conflict.ConflictingRange2}");
                            await writer.WriteLineAsync();
                        }

                        await writer.WriteLineAsync("----------------------------------------------------------------------------------------------------");
                        await writer.WriteLineAsync();
                    }
                }

                var cityConflicts = report.Conflicts.Where(c => !c.IsStreetLevel).GroupBy(c => c.Miasto);

                if (cityConflicts.Any())
                {
                    await writer.WriteLineAsync("╔═══════════════════════════════════════════════════════════════════════════════════════════════╗");
                    await writer.WriteLineAsync("║ KONFLIKTY NA POZIOMIE MIAST (bez konkretnej ulicy)                                           ║");
                    await writer.WriteLineAsync("╚═══════════════════════════════════════════════════════════════════════════════════════════════╝");
                    await writer.WriteLineAsync();

                    foreach (var group in cityConflicts)
                    {
                        await writer.WriteLineAsync($"Miejscowość: {group.Key}");
                        await writer.WriteLineAsync();

                        foreach (var conflict in group)
                        {
                            await writer.WriteLineAsync("  ⚠️ KONFLIKT:");
                            await writer.WriteLineAsync($"     Kod 1: {conflict.Kod1} → zakresy: {conflict.Numery1}");
                            await writer.WriteLineAsync($"     Kod 2: {conflict.Kod2} → zakresy: {conflict.Numery2}");
                            await writer.WriteLineAsync("     Nakładające się zakresy:");
                            await writer.WriteLineAsync($"       • {conflict.ConflictingRange1}");
                            await writer.WriteLineAsync($"       • {conflict.ConflictingRange2}");
                            await writer.WriteLineAsync();
                        }

                        await writer.WriteLineAsync("----------------------------------------------------------------------------------------------------");
                        await writer.WriteLineAsync();
                    }
                }
            }

            await writer.WriteLineAsync();
            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync("KONIEC RAPORTU");
            await writer.WriteLineAsync("====================================================================================================");
        }

        private async Task SaveWarningsAsync()
        {
            if (!_parityWarnings.Any())
            {
                return;
            }

            var logPath = Path.Combine(_logDirectory, "VerifyPostalCodesWarnings.txt");
            Directory.CreateDirectory(_logDirectory);

            using var writer = new StreamWriter(logPath, false, System.Text.Encoding.UTF8);

            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync("⚠️  OSTRZEŻENIA: AUTOMATYCZNE DODAWANIE PARZYSTOŚCI");
            await writer.WriteLineAsync($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"Znaleziono ostrzeżeń: {_parityWarnings.Count:N0}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("Następujące zakresy nie mają jawnego oznaczenia (n)/(p), ale zostały automatycznie");
            await writer.WriteLineAsync("sklasyfikowane na podstawie parzystości numeru/początku i końca zakresu:");
            await writer.WriteLineAsync("UWAGA: Zakresy <numer>-DK bez oznaczenia (n)/(p) oznaczają WSZYSTKIE numery!");
            await writer.WriteLineAsync();

            var grouped = _parityWarnings
                .GroupBy(w => new { w.Miasto, w.Ulica })
                .OrderBy(g => g.Key.Miasto)
                .ThenBy(g => g.Key.Ulica ?? "");

            foreach (var group in grouped)
            {
                await writer.WriteLineAsync($"Miejscowość: {group.Key.Miasto}");
                if (!string.IsNullOrEmpty(group.Key.Ulica))
                {
                    await writer.WriteLineAsync($"Ulica: {group.Key.Ulica}");
                }
                await writer.WriteLineAsync();

                foreach (var warning in group)
                {
                    await writer.WriteLineAsync($"  ⚠️  Kod: {warning.Kod}");
                    await writer.WriteLineAsync($"     Oryginalny: {warning.OriginalRange}");
                    await writer.WriteLineAsync($"     Sugerowany: {warning.SuggestedRange}");
                    await writer.WriteLineAsync($"     Powód: {warning.Reason}");
                    await writer.WriteLineAsync();
                }

                await writer.WriteLineAsync("----------------------------------------------------------------------------------------------------");
                await writer.WriteLineAsync();
            }

            await writer.WriteLineAsync("====================================================================================================");
            await writer.WriteLineAsync("KONIEC OSTRZEŻEŃ");
            await writer.WriteLineAsync("====================================================================================================");
        }
    }

    public class ValidationReport
    {
        public int ProcessedStreets { get; set; }
        public int ProcessedCities { get; set; }
        public int TotalConflicts { get; set; }
        public double ElapsedSeconds { get; set; }
        public List<RangeConflict> Conflicts { get; set; } = new();
    }

    public class RangeConflict
    {
        public string Kod1 { get; set; } = string.Empty;
        public string Kod2 { get; set; } = string.Empty;
        public string Ulica { get; set; } = string.Empty;
        public string Dzielnica { get; set; } = string.Empty;
        public string Miasto { get; set; } = string.Empty;
        public string Numery1 { get; set; } = string.Empty;
        public string Numery2 { get; set; } = string.Empty;
        public string ConflictingRange1 { get; set; } = string.Empty;
        public string ConflictingRange2 { get; set; } = string.Empty;
        public bool IsStreetLevel { get; set; } = false;
    }

    public class ParityWarning
    {
        public string Kod { get; set; } = string.Empty;
        public string? Miasto { get; set; } = string.Empty;
        public string? Ulica { get; set; } = string.Empty;
        public string OriginalRange { get; set; } = string.Empty;
        public string SuggestedRange { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public struct NumberRange
    {
        public int Start { get; set; }
        public int End { get; set; } 
        public ParityType Type { get; set; }
    }

    public enum ParityType
    {
        All,
        Even,
        Odd
    }
}