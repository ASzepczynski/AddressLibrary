using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class AdresConfiguration : IEntityTypeConfiguration<Adres>
    {
        public void Configure(EntityTypeBuilder<Adres> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}
