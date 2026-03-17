using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class UlicaConfiguration : IEntityTypeConfiguration<Ulica>
    {
        public void Configure(EntityTypeBuilder<Ulica> builder)
        {
            // ✅ DODANO: Jawnie ignoruj Nazwa1 i Nazwa2 (są NotMapped, ale dla jasności)
            builder.Ignore(e => e.Nazwa1);
            builder.Ignore(e => e.Nazwa2);

            // Indeks unikalny na Symbol + MiastoId + Dzielnica (symbol ulicy jest unikalny w kontekście miejscowości)
            builder.HasIndex(e => new { e.Symbol, e.MiastoId, e.Dzielnica }).IsUnique();

            // Indeks na TypUlicyId dla szybszego wyszukiwania
            builder.HasIndex(e => e.TypUlicyId);

            // Relacja do Miasta (DeleteBehavior.Restrict)
            builder.HasOne(e => e.Miasto)
                  .WithMany(m => m.Ulice)
                  .HasForeignKey(e => e.MiastoId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relacja do TypUlicy (opcjonalna, DeleteBehavior.SetNull)
            builder.HasOne(e => e.TypUlicy)
                  .WithMany()
                  .HasForeignKey(e => e.TypUlicyId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        }
    }
}