using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla tabeli TypyUlic
    /// </summary>
    public class TypyUlicConfiguration : IEntityTypeConfiguration<TypUlicy>
    {
        public void Configure(EntityTypeBuilder<TypUlicy> builder)
        {
            builder.ToTable("TypyUlic");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Prefiks)
                .HasMaxLength(20)
                .IsRequired(false)
                .HasComment("Prefiks nazwy ulicy (np. p³k., gen., ks., im., imienia)");

            builder.Property(t => t.Tytul)
                .HasMaxLength(50)
                .IsRequired(false)
                .HasComment("Tytu³ osoby (np. dr., prof., p³k.)");

            builder.Property(t => t.Imie)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Pierwsze imiê patrona ulicy");

            builder.Property(t => t.Imie2)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Drugie imiê patrona ulicy");

            builder.Property(t => t.Nazwisko)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Pierwsze nazwisko patrona ulicy");

            builder.Property(t => t.Nazwisko2)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Drugie nazwisko patrona ulicy");

            builder.Property(t => t.Postfiks)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Postfiks/przydomek (np. Zapory, Zoœki)");

            builder.Property(t => t.Original)
                .HasMaxLength(500)
                .IsRequired(false)
                .HasComment("Oryginalna pe³na nazwa ulicy: Cecha + Nazwa2 + Nazwa1");

            // Indeksy dla wydajnoœci wyszukiwania
            builder.HasIndex(t => t.Nazwisko)
                .HasDatabaseName("IX_TypyUlic_Nazwisko");

            builder.HasIndex(t => t.Original)
                .HasDatabaseName("IX_TypyUlic_Original");
        }
    }
}