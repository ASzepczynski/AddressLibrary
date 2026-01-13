using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class GminaConfiguration : IEntityTypeConfiguration<Gmina>
    {
        public void Configure(EntityTypeBuilder<Gmina> builder)
        {
            // POPRAWIONO: Indeks kompozytowy (PowiatId + Kod + RodzajGminyId)
            // Ten sam kod gminy mo¿e wyst¹piæ w ró¿nych powiatach
            builder.HasIndex(e => new { e.PowiatId, e.Kod, e.RodzajGminyId })
                   .IsUnique()
                   .HasDatabaseName("IX_Gminy_PowiatId_Kod_RodzajGminyId");
            
            builder.HasIndex(e => e.Nazwa);

            // DeleteBehavior
            builder.HasOne(e => e.Powiat)
                  .WithMany(p => p.Gminy)
                  .HasForeignKey(e => e.PowiatId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.RodzajGminy)
                  .WithMany(r => r.Gminy)
                  .HasForeignKey(e => e.RodzajGminyId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}