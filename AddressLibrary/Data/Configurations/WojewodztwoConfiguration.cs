using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class WojewodztwoConfiguration : IEntityTypeConfiguration<Wojewodztwo>
    {
        public void Configure(EntityTypeBuilder<Wojewodztwo> builder)
        {
            // Indeksy (nie da siê zrobiæ atrybutem)
            builder.HasIndex(e => e.Kod).IsUnique();
            builder.HasIndex(e => e.Nazwa);
        }
    }
}