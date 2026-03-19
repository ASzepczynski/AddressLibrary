using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class TerytUlicConfiguration : IEntityTypeConfiguration<TerytUlic>
    {
        public void Configure(EntityTypeBuilder<TerytUlic> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}