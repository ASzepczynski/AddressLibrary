using AddressLibrary.Data;
using AddressLibrary.Models;
using System.Text;

namespace AddressLibrary.Services
{
    public class PnaCsvLoader
    {
        private readonly AddressDbContext _context;
        private readonly string? _appDataPath;

        public PnaCsvLoader(AddressDbContext context, string? appDataPath = null)
        {
            _context = context;
            _appDataPath = appDataPath;
        }

        /// <summary>
        /// £aduje dane PNA z pliku CSV zakodowanego w CP-1250
        /// Format: KOD;MIEJSCOWOŒÆ;ULICA;NUMERY;GMINA;POWIAT;WOJEWÓDZTWO
        /// Pierwsza linia to nag³ówek (jest pomijana)
        /// Jeœli miejscowoœæ zawiera nazwê w nawiasach, wyodrêbnia j¹ do pola Dzielnica
        /// </summary>
        public async Task LoadFromCsvAsync(string csvFilePath, IProgress<LoadProgressInfo>? progress = null)
        {
            if (!System.IO.File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"Nie znaleziono pliku: {csvFilePath}");
            }

            // Rejestruj kodowanie CP-1250 (Windows-1250)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(1250);

            var lines = await System.IO.File.ReadAllLinesAsync(csvFilePath, encoding);
            
            var totalLines = lines.Length;
            var processedLines = 0;
            var emptyLines = 0;
            var invalidLines = 0;
            var dzielnicaCount = 0; // Licznik miejsc z dzielnic¹
            var batchSize = 1000;
            var pnaBatch = new List<Pna>();

            progress?.Report(new LoadProgressInfo
            {
                ProcessedRecords = 0,
                TotalRecords = totalLines - 1, // Odejmij nag³ówek
                CurrentAction = $"Rozpoczynam przetwarzanie {totalLines - 1} linii (pomijam nag³ówek)..."
            });

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Pomiñ pierwsz¹ liniê (nag³ówek)
                if (i == 0)
                {
                    progress?.Report(new LoadProgressInfo
                    {
                        ProcessedRecords = 0,
                        TotalRecords = totalLines - 1,
                        CurrentAction = $"Nag³ówek: '{line}'\nPomijam i przechodzê do danych..."
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    emptyLines++;
                    continue;
                }

                // Parsuj liniê CSV (separator: œrednik)
                var parts = line.Split(';');
                
                // Loguj drug¹ liniê (pierwsz¹ z danymi) do diagnostyki
                if (i == 1)
                {
                    progress?.Report(new LoadProgressInfo
                    {
                        ProcessedRecords = 0,
                        TotalRecords = totalLines - 1,
                        CurrentAction = $"Przyk³adowa linia danych [1]: '{line.Substring(0, Math.Min(150, line.Length))}'\nLiczba kolumn: {parts.Length}\nFormat: KOD;MIEJSCOWOŒÆ;ULICA;NUMERY;GMINA;POWIAT;WOJEWÓDZTWO"
                    });
                }

                // SprawdŸ czy linia ma 7 kolumn (zgodnie z formatem CSV)
                if (parts.Length < 7)
                {
                    invalidLines++;
                    // Loguj kilka pierwszych nieprawid³owych linii
                    if (invalidLines <= 5)
                    {
                        progress?.Report(new LoadProgressInfo
                        {
                            ProcessedRecords = processedLines,
                            TotalRecords = totalLines - 1,
                            CurrentAction = $"Pominiêto liniê {i}: tylko {parts.Length} kolumn (wymagane 7)\nLinia: {line.Substring(0, Math.Min(150, line.Length))}"
                        });
                    }
                    continue;
                }

                // Format CSV: KOD;MIEJSCOWOŒÆ;ULICA;NUMERY;GMINA;POWIAT;WOJEWÓDZTWO
                var miejscowoscRaw = parts[1].Trim();
                var (miejscowosc, dzielnica) = ParseMiejscowoscZDzielnica(miejscowoscRaw);
                
                if (!string.IsNullOrEmpty(dzielnica))
                {
                    dzielnicaCount++;
                }

                var pna = new Pna
                {
                    Kod = parts[0].Trim(),
                    Miasto = miejscowosc,               // MIEJSCOWOŒÆ (bez dzielnicy)
                    Dzielnica = dzielnica,              // DZIELNICA (wyodrêbniona z nawiasów)
                    Ulica = parts[2].Trim(),            // ULICA
                    Numery = parts[3].Trim(),           // NUMERY
                    Gmina = parts[4].Trim(),            // GMINA
                    Powiat = parts[5].Trim(),           // POWIAT
                    Wojewodztwo = parts[6].Trim()       // WOJEWÓDZTWO
                };

                pnaBatch.Add(pna);

                if (pnaBatch.Count >= batchSize)
                {
                    await _context.Pna.AddRangeAsync(pnaBatch);
                    await _context.SaveChangesAsync();
                    
                    processedLines += pnaBatch.Count;
                    progress?.Report(new LoadProgressInfo
                    {
                        ProcessedRecords = processedLines,
                        TotalRecords = totalLines - 1,
                        CurrentAction = $"Za³adowano {processedLines} / {totalLines - 1} rekordów (dzielnic: {dzielnicaCount})"
                    });

                    pnaBatch.Clear();
                }
            }

            // Zapisz pozosta³e rekordy
            if (pnaBatch.Any())
            {
                await _context.Pna.AddRangeAsync(pnaBatch);
                await _context.SaveChangesAsync();
                
                processedLines += pnaBatch.Count;
            }

            // Raport koñcowy
            progress?.Report(new LoadProgressInfo
            {
                ProcessedRecords = processedLines,
                TotalRecords = totalLines - 1,
                CurrentAction = $"Zakoñczono!\nPrzetworzone: {processedLines}\nZ dzielnic¹: {dzielnicaCount}\nPuste linie: {emptyLines}\nNieprawid³owe: {invalidLines}\nRazem linii (bez nag³ówka): {totalLines - 1}"
            });
        }

        /// <summary>
        /// Wyodrêbnia miejscowoœæ i dzielnicê z tekstu w formacie "Miejscowoœæ (Dzielnica)"
        /// </summary>
        /// <param name="miejscowoscRaw">Surowy tekst miejscowoœci</param>
        /// <returns>Krotka (miejscowoœæ, dzielnica)</returns>
        private static (string miejscowosc, string dzielnica) ParseMiejscowoscZDzielnica(string miejscowoscRaw)
        {
            if (string.IsNullOrWhiteSpace(miejscowoscRaw))
            {
                return (string.Empty, string.Empty);
            }

            // Szukaj nawiasu otwieraj¹cego
            var openParenIndex = miejscowoscRaw.IndexOf('(');
            var closeParenIndex = miejscowoscRaw.IndexOf(')');

            // Jeœli nie ma nawiasów lub s¹ nieprawid³owe, zwróæ oryginalny tekst
            if (openParenIndex == -1 || closeParenIndex == -1 || closeParenIndex <= openParenIndex)
            {
                return (miejscowoscRaw, string.Empty);
            }

            // Wyodrêbnij miejscowoœæ (przed nawiasem) i dzielnicê (w nawiasie)
            var miejscowosc = miejscowoscRaw.Substring(0, openParenIndex).Trim();
            var dzielnica = miejscowoscRaw.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();

            return (miejscowosc, dzielnica);
        }

        public class LoadProgressInfo
        {
            public int ProcessedRecords { get; set; }
            public int TotalRecords { get; set; }
            public string CurrentAction { get; set; } = string.Empty;
        }
    }
}