using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class UlicaConfiguration : IEntityTypeConfiguration<Ulica>
    {
        public void Configure(EntityTypeBuilder<Ulica> builder)
        {
            // Indeks unikalny na Symbol + MiastoId (symbol ulicy jest unikalny w kontekœcie miejscowoœci)
            builder.HasIndex(e => new { e.Symbol, e.MiastoId }).IsUnique();
            
            // Indeks na Nazwa1 dla wyszukiwania
            builder.HasIndex(e => e.Nazwa1);

            // DeleteBehavior
            builder.HasOne(e => e.Miasto)
                  .WithMany(m => m.Ulice)
                  .HasForeignKey(e => e.MiastoId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}