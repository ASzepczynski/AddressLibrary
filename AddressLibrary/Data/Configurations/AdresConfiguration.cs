using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class AdresConfiguration : IEntityTypeConfiguration<Adres>
    {
        public void Configure(EntityTypeBuilder<Adres> builder)
        {
            builder.ToTable("Adresy");

            builder.HasKey(a => a.Id);

            // Wymuszenie nvarchar dla wszystkich kolumn tekstowych
            builder.Property(a => a.Id)
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            builder.Property(a => a.Kraj)
                .HasColumnType("nvarchar(100)");

            builder.Property(a => a.Kod)
                .HasColumnType("nvarchar(10)");

            builder.Property(a => a.Miasto)
                .HasColumnType("nvarchar(200)");

            builder.Property(a => a.Ulica)
                .HasColumnType("nvarchar(200)");

            builder.Property(a => a.NrDomu)
                .HasColumnType("nvarchar(20)");

            builder.Property(a => a.NrLokalu)
                .HasColumnType("nvarchar(20)");

            builder.Property(a => a.Wojewodztwo)
                .HasColumnType("nvarchar(100)");

            builder.Property(a => a.Powiat)
                .HasColumnType("nvarchar(100)");

            builder.Property(a => a.Gmina)
                .HasColumnType("nvarchar(100)");

            // Indeksy dla szybszego wyszukiwania
            builder.HasIndex(a => a.Kod);
            builder.HasIndex(a => new { a.Miasto, a.Kod });
        }
    }
}
