using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class UlicaConfiguration : IEntityTypeConfiguration<Ulica>
    {
        public void Configure(EntityTypeBuilder<Ulica> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}