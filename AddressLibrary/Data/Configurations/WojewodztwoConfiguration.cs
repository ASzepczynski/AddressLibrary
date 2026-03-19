using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class WojewodztwoConfiguration : IEntityTypeConfiguration<Wojewodztwo>
    {
        public void Configure(EntityTypeBuilder<Wojewodztwo> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}