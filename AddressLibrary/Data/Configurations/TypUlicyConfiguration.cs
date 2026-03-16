using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla modelu TypUlicy
    /// </summary>
    public class TypUlicyConfiguration : IEntityTypeConfiguration<TypUlicy>
    {
        public void Configure(EntityTypeBuilder<TypUlicy> builder)
        {
            builder.ToTable("TypyUlic");

            builder.HasKey(t => t.Id);

            // Kolumny tekstowe z ograniczeniami d³ugoœci
            builder.Property(t => t.Prefiks)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Prefiks nazwy ulicy (im., Leœny, Miejski)");

            builder.Property(t => t.Tytul)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .HasComment("Tytu³ osoby (np. dr., prof., p³k.)");

            builder.Property(t => t.Imie)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Pierwsze imiê patrona ulicy");

            builder.Property(t => t.Imie2)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Drugie imiê patrona ulicy (np. Kamil w Krzysztofa Kamila Baczyñskiego)");

            builder.Property(t => t.Nazwisko)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Pierwsze nazwisko patrona ulicy");

            builder.Property(t => t.Nazwisko2)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Drugie nazwisko patrona ulicy (np. Reymonta w W³adys³awa Stanis³awa Reymonta)");

            builder.Property(t => t.Pseudonim)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Pseudonim patrona ulicy (np. Zapory, Zoœki, Nila)");

            builder.Property(t => t.Postfiks)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .HasComment("Postfiks/przydomek (dodatkowe informacje)");

            // Indeks na nazwisku dla szybszego wyszukiwania
            builder.HasIndex(t => t.Nazwisko)
                .HasDatabaseName("IX_TypyUlic_Nazwisko");

            // Indeks kompozytowy dla wyszukiwania po imieniu i nazwisku
            builder.HasIndex(t => new { t.Imie, t.Nazwisko })
                .HasDatabaseName("IX_TypyUlic_Imie_Nazwisko");
        }
    }
}