using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class KodPocztowyConfiguration : IEntityTypeConfiguration<KodPocztowy>
    {
        public void Configure(EntityTypeBuilder<KodPocztowy> builder)
        {
            builder.SetAllColumnsCaseSensitive();

            // Relacje z Restrict aby unikn¹æ cyklicznych œcie¿ek kaskadowych
            builder.HasOne(k => k.Miasto)
                .WithMany(m => m.KodyPocztowe)
                .HasForeignKey(k => k.MiastoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(k => k.Ulica)
                .WithMany(u => u.KodyPocztowe)
                .HasForeignKey(k => k.UlicaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}