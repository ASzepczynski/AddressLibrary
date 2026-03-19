using AddressLibrary.Helpers;
using AddressLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class TerytSimcConfiguration : IEntityTypeConfiguration<TerytSimc>
    {
        public void Configure(EntityTypeBuilder<TerytSimc> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}