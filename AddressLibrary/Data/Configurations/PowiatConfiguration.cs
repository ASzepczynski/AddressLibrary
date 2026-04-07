using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class PowiatConfiguration : IEntityTypeConfiguration<Powiat>
    {
        public void Configure(EntityTypeBuilder<Powiat> builder)
        {
            builder.ApplyStandardConfiguration();
        }
    }
}