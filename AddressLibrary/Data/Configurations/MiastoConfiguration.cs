using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class MiastoConfiguration : IEntityTypeConfiguration<Miasto>
    {
        public void Configure(EntityTypeBuilder<Miasto> builder)
        {
            builder.ApplyStandardConfiguration();
        }
    }
}