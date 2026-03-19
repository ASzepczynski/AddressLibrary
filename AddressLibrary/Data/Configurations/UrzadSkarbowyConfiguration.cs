using AddressLibrary.Models;
using AddressLibrary.Helpers;
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
            builder.SetAllColumnsCaseSensitive();
        }
    }
}