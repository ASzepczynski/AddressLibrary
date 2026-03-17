using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    /// <summary>
    /// Konfiguracja Entity Framework dla tabeli TerytUlicPoprawki
    /// </summary>
    public class TerytUlicPoprawkiConfiguration : IEntityTypeConfiguration<TerytUlicPoprawka>
    {
        public void Configure(EntityTypeBuilder<TerytUlicPoprawka> builder)
        {
            builder.ToTable("TerytUlicPoprawki");

            // ✅ POPRAWIONE: DbId jest kluczem głównym
            builder.HasKey(t => t.DbId);

            // ✅ DODANO: Konfiguracja dla pola Cecha
            builder.Property(t => t.Cecha)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Cecha ulicy (np. ul., al., pl.)");

            builder.Property(t => t.Prefiks)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Prefiks nazwy ulicy (imienia, leśny)");

            builder.Property(t => t.Tytul)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Tytuł osoby (np. dr., prof., płk.)");

            builder.Property(t => t.Imie)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Pierwsze imię patrona ulicy");

            builder.Property(t => t.Imie2)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Drugie imię patrona ulicy");

            builder.Property(t => t.Nazwisko)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Pierwsze nazwisko patrona ulicy");

            builder.Property(t => t.Nazwisko2)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Drugie nazwisko patrona ulicy");

            builder.Property(t => t.Pseudonim)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Pseudonim patrona ulicy (np. Zapory, Zośki, Nila)");

            builder.Property(t => t.Postfiks)
                .HasMaxLength(100)
                .IsRequired(false)
                .HasComment("Postfiks/przydomek (dodatkowe informacje)");

            // ✅ POPRAWIONE: Original zmienione na Id (klucz biznesowy)
            builder.Property(t => t.Id)
                .HasMaxLength(500)
                .IsRequired(true)
                .HasComment("Identyfikator/klucz biznesowy - oryginalna pełna nazwa ulicy: Cecha + Nazwa2 + Nazwa1");

            // Indeksy dla wydajności wyszukiwania
            builder.HasIndex(t => t.Nazwisko)
                .HasDatabaseName("IX_TerytUlicPoprawki_Nazwisko");

            // ✅ POPRAWIONE: Indeks na Id (był Original)
            builder.HasIndex(t => t.Id)
                .HasDatabaseName("IX_TerytUlicPoprawki_Id");
        }
    }
}