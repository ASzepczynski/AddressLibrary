using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla tabeli UrzedySkarbowe
    /// </summary>
    public class UrzadSkarbowyConfiguration : IEntityTypeConfiguration<UrzadSkarbowy>
    {
        public void Configure(EntityTypeBuilder<UrzadSkarbowy> builder)
        {
            // Nazwa tabeli
            builder.ToTable("UrzedySkarbowe");

            // Klucz g³ówny
            builder.HasKey(e => e.Id);

            // Konfiguracja w³aœciwoœci
            builder.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            builder.Property(e => e.Nazwa)
                .HasColumnType("nvarchar(200)")
                .IsRequired();

            builder.Property(e => e.Kod)
                .HasColumnType("nvarchar(10)");

            builder.Property(e => e.Miasto)
                .HasColumnType("nvarchar(100)");

            builder.Property(e => e.Ulica)
                .HasColumnType("nvarchar(200)");

            builder.Property(e => e.NrDomu)
                .HasColumnType("nvarchar(20)");

            builder.Property(e => e.Email)
                .HasColumnType("nvarchar(100)");

            builder.Property(e => e.Www)
                .HasColumnType("nvarchar(200)");

            // Relacja do Ulicy (opcjonalna)
            builder.HasOne(e => e.UlicaNavigation)
                .WithMany()
                .HasForeignKey(e => e.UlicaId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indeksy dla wydajnoœci wyszukiwania
            builder.HasIndex(e => e.Nazwa);
            builder.HasIndex(e => e.Miasto);
            builder.HasIndex(e => e.Kod);
            builder.HasIndex(e => e.UlicaId);
            builder.HasIndex(e => new { e.Miasto, e.Ulica });
        }
    }
}