using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class UlicaConfiguration : IEntityTypeConfiguration<Ulica>
    {
        public void Configure(EntityTypeBuilder<Ulica> builder)
        {
            // Indeks unikalny na Symbol + MiastoId (symbol ulicy jest unikalny w kontekście miejscowości)
            builder.HasIndex(e => new { e.Symbol, e.MiastoId, e.Dzielnica }).IsUnique();

            // Indeks na Nazwa1 dla wyszukiwania
            builder.HasIndex(e => e.Nazwa1);

            // Indeks na TypUlicyId dla szybszego wyszukiwania
            builder.HasIndex(e => e.TypUlicyId);

            // Relacja do Miasta (DeleteBehavior.Restrict)
            builder.HasOne(e => e.Miasto)
                  .WithMany(m => m.Ulice)
                  .HasForeignKey(e => e.MiastoId)
                  .OnDelete(DeleteBehavior.Restrict);

            // ✅ DODANO: Relacja do TypUlicy (opcjonalna, DeleteBehavior.SetNull)
            builder.HasOne(e => e.TypUlicy)
                  .WithMany()
                  .HasForeignKey(e => e.TypUlicyId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        }
    }
}