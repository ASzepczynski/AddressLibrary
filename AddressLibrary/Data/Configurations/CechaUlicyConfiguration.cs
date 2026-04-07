using AddressLibrary.Models;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class CechaUlicyConfiguration : IEntityTypeConfiguration<CechaUlicy>
    {
        public void Configure(EntityTypeBuilder<CechaUlicy> builder)
        {
            builder.ApplyStandardConfiguration();
        }
    }
}