using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class KodPocztowyConfiguration : IEntityTypeConfiguration<KodPocztowy>
    {
        public void Configure(EntityTypeBuilder<KodPocztowy> builder)
        {
            // Indeks na kolumnie Kod (nie da siê zrobiæ atrybutem)
            builder.HasIndex(e => e.Kod);

            // DeleteBehavior.Restrict (nie da siê zrobiæ atrybutem)
            builder.HasOne(e => e.Miejscowosc)
                  .WithMany(m => m.KodyPocztowe)
                  .HasForeignKey(e => e.MiejscowoscId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Ulica)
                  .WithMany(u => u.KodyPocztowe)
                  .HasForeignKey(e => e.UlicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}