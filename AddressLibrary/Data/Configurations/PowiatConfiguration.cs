using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class PowiatConfiguration : IEntityTypeConfiguration<Powiat>
    {
        public void Configure(EntityTypeBuilder<Powiat> builder)
        {
            // POPRAWIONO: Indeks kompozytowy (WojewodztwoId + Kod)
            // Ten sam kod powiatu mo¿e wyst¹piæ w ró¿nych województwach
            builder.HasIndex(e => new { e.WojewodztwoId, e.Kod })
                   .IsUnique()
                   .HasDatabaseName("IX_Powiaty_WojewodztwoId_Kod");
            
            builder.HasIndex(e => e.Nazwa);

            // DeleteBehavior
            builder.HasOne(e => e.Wojewodztwo)
                  .WithMany(w => w.Powiaty)
                  .HasForeignKey(e => e.WojewodztwoId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}