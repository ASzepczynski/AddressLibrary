using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class RodzajMiastaConfiguration : IEntityTypeConfiguration<RodzajMiasta>
    {
        public void Configure(EntityTypeBuilder<RodzajMiasta> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(e => e.Kod).HasMaxLength(10).IsRequired();
            builder.Property(e => e.Nazwa).HasMaxLength(100).IsRequired();
            builder.HasIndex(e => e.Kod).IsUnique();

            // Seed data przeniesione do RodzajeMiastLoader
        }
    }
}