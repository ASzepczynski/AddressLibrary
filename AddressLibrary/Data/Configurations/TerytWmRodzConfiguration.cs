using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class TerytWmRodzConfiguration : IEntityTypeConfiguration<TerytWmRodz>
    {
        public void Configure(EntityTypeBuilder<TerytWmRodz> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}