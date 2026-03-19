using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla tabeli TerytUlicPoprawki
    /// </summary>
    public class TerytUlicPoprawkiConfiguration : IEntityTypeConfiguration<TerytUlicPoprawka>
    {
        public void Configure(EntityTypeBuilder<TerytUlicPoprawka> builder)
        {
            builder.SetAllColumnsCaseSensitive();
            // Indeksy dla wydajności wyszukiwania
            builder.HasIndex(t => t.Nazwisko)
                .HasDatabaseName("IX_TerytUlicPoprawki_Nazwisko");

            // ✅ POPRAWIONE: Indeks na Id (był Original)
            builder.HasIndex(t => t.Id)
                .HasDatabaseName("IX_TerytUlicPoprawki_Id");
        }
    }
}