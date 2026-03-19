using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class TytulStopienConfiguration : IEntityTypeConfiguration<TytulStopien>
    {
        public void Configure(EntityTypeBuilder<TytulStopien> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}