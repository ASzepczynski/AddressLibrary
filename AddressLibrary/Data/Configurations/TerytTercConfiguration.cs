using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class TerytTercConfiguration : IEntityTypeConfiguration<TerytTerc>
    {
        public void Configure(EntityTypeBuilder<TerytTerc> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}