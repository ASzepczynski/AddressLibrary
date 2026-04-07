using AddressLibrary.Models;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class RodzajGminyConfiguration : IEntityTypeConfiguration<RodzajGminy>
    {
        public void Configure(EntityTypeBuilder<RodzajGminy> builder)
        {
            builder.ApplyStandardConfiguration();
        }
    }
}