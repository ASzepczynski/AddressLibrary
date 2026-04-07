using AddressLibrary.Models;
using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class RodzajMiastaConfiguration : IEntityTypeConfiguration<RodzajMiasta>
    {
        public void Configure(EntityTypeBuilder<RodzajMiasta> builder)
        {
            builder.ApplyStandardConfiguration();
        }
    }
}