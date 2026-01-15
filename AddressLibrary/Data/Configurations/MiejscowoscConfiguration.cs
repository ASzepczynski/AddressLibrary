using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class MiastoConfiguration : IEntityTypeConfiguration<Miasto>
    {
        public void Configure(EntityTypeBuilder<Miasto> builder)
        {
            // Indeksy
            builder.HasIndex(e => e.Symbol).IsUnique();
            builder.HasIndex(e => e.Nazwa);

            // DeleteBehavior
            builder.HasOne(e => e.Gmina)
                  .WithMany(g => g.Miasta)
                  .HasForeignKey(e => e.GminaId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.RodzajMiasta)
                  .WithMany(r => r.Miasta)
                  .HasForeignKey(e => e.RodzajMiastaId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}