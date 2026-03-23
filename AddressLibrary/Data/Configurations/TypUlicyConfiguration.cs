using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla modelu TypUlicy
    /// </summary>
    public class TypUlicyConfiguration : IEntityTypeConfiguration<TypUlicy>
    {
        public void Configure(EntityTypeBuilder<TypUlicy> builder)
        {
            builder.SetAllColumnsCaseSensitive();

            // Indeksy dla szybszego wyszukiwania
            builder.HasIndex(t => t.TytulStopienId)
                .HasDatabaseName("IX_TypyUlic_TytulStopienId");
        }
    }
}