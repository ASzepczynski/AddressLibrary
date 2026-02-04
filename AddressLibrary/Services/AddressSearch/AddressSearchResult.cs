// Copyright (c) 2025-2026 Andrzej Szepczyński. All rights reserved.

using AddressLibrary.Models;
using System.Text;

namespace AddressLibrary.Services.AddressSearch
{
    /// <summary>
    /// Status wyszukiwania adresu
    /// </summary>
    public enum AddressSearchStatus
    {
        Success,              // Znaleziono dokładny adres
        MultipleMatches,      // Znaleziono wiele pasujących adresów
        MiastoNotFound,       // Nie znaleziono miejscowości
        UlicaNotFound,        // Nie znaleziono ulicy
        InvalidStreetName,    // Błędna nazwa ulicy (nie istnieje w całej bazie TERYT)
        KodPocztowyNotFound,  // Nie znaleziono kodu pocztowego
        ValidationError       // Błąd walidacji danych wejściowych
    }

    /// <summary>
    /// Wynik wyszukiwania adresu
    /// </summary>
    public class AddressSearchResult
    {
        public AddressSearchStatus Status { get; set; }
        public string? Message { get; set; }

        // Znalezione dane
        public KodPocztowy? KodPocztowy { get; set; }
        public Miasto? Miasto { get; set; }
        public Ulica? Ulica { get; set; }

        // Znormalizowane numery (z uwzględnieniem numerów wyciągniętych z nazwy ulicy)
        public string? NormalizedBuildingNumber { get; set; }
        public string? NormalizedApartmentNumber { get; set; }

        // W przypadku wielu dopasowań
        public List<KodPocztowy>? AlternativeMatches { get; set; }

        // ✅ POPRAWIONE: Informacje diagnostyczne dla tego konkretnego wyszukiwania
        private List<string> _diagnosticMessages = new();
        
        /// <summary>
        /// Dodaje wiadomość diagnostyczną
        /// </summary>
        public void AddDiagnostic(string message)
        {
            _diagnosticMessages.Add(message);
        }

        /// <summary>
        /// Zwraca wszystkie informacje diagnostyczne jako jeden string
        /// </summary>
        public string? DiagnosticInfo => _diagnosticMessages.Count > 0 
            ? string.Join(Environment.NewLine, _diagnosticMessages) 
            : null;

        /// <summary>
        /// Tworzy podsumowanie diagnostyczne w formacie czytelnym dla człowieka
        /// </summary>
        public string GetFormattedDiagnostics()
        {
            if (_diagnosticMessages.Count == 0)
                return "Brak informacji diagnostycznych";

            var sb = new StringBuilder();
            sb.AppendLine("=== Informacje diagnostyczne ===");
            
            foreach (var msg in _diagnosticMessages)
            {
                sb.AppendLine($"  • {msg}");
            }

            return sb.ToString();
        }
    }
}