// Copyright (c) 2025-2026 Andrzej Szepczyñski. All rights reserved.

namespace AddressLibrary.Models
{
    /// <summary>
    /// Po³¹czenie kodu pocztowego z pe³nymi danymi adresowymi
    /// </summary>
    public class KodPocztowyZAdresem
    {
        public KodPocztowy KodPocztowy { get; set; } = null!;
        public Miejscowosc? Miejscowosc { get; set; }
        public Ulica? Ulica { get; set; }

        /// <summary>
        /// Generuje pe³ny adres w formacie tekstowym
        /// </summary>
        public string PelnyAdres
        {
            get
            {
                var parts = new List<string>();

                // Kod pocztowy
                if (!string.IsNullOrEmpty(KodPocztowy?.Kod))
                {
                    parts.Add(KodPocztowy.Kod);
                }

                // Miejscowoœæ
                if (!string.IsNullOrEmpty(Miejscowosc?.Nazwa))
                {
                    parts.Add(Miejscowosc.Nazwa);
                }

                // Ulica (z kombinacj¹ Nazwa2 + Nazwa1 jeœli istniej¹ obie)
                if (Ulica != null)
                {
                    if (!string.IsNullOrEmpty(Ulica.Nazwa2) && !string.IsNullOrEmpty(Ulica.Nazwa1))
                    {
                        parts.Add($"{Ulica.Nazwa2} {Ulica.Nazwa1}");
                    }
                    else if (!string.IsNullOrEmpty(Ulica.Nazwa1))
                    {
                        parts.Add(Ulica.Nazwa1);
                    }
                }

                // Numery
                if (!string.IsNullOrEmpty(KodPocztowy?.Numery))
                {
                    parts.Add($"({KodPocztowy.Numery})");
                }

                // Województwo, Powiat, Gmina
                var locationParts = new List<string>();
                if (!string.IsNullOrEmpty(Miejscowosc?.Gmina?.Powiat?.Wojewodztwo?.Nazwa))
                {
                    locationParts.Add($"woj. {Miejscowosc.Gmina.Powiat.Wojewodztwo.Nazwa}");
                }
                if (!string.IsNullOrEmpty(Miejscowosc?.Gmina?.Powiat?.Nazwa))
                {
                    locationParts.Add($"pow. {Miejscowosc.Gmina.Powiat.Nazwa}");
                }
                if (!string.IsNullOrEmpty(Miejscowosc?.Gmina?.Nazwa))
                {
                    locationParts.Add($"gm. {Miejscowosc.Gmina.Nazwa}");
                }

                if (locationParts.Any())
                {
                    parts.Add($"[{string.Join(", ", locationParts)}]");
                }

                return string.Join(", ", parts);
            }
        }
    }
}