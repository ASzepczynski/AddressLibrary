using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class MiejscowoscConfiguration : IEntityTypeConfiguration<Miejscowosc>
    {
        public void Configure(EntityTypeBuilder<Miejscowosc> builder)
        {
            // Indeksy
            builder.HasIndex(e => e.Symbol).IsUnique();
            builder.HasIndex(e => e.Nazwa);

            // DeleteBehavior
            builder.HasOne(e => e.Gmina)
                  .WithMany(g => g.Miejscowosci)
                  .HasForeignKey(e => e.GminaId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.RodzajMiejscowosci)
                  .WithMany(r => r.Miejscowosci)
                  .HasForeignKey(e => e.RodzajMiejscowosciId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}