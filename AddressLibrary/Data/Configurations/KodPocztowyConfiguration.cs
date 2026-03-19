using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class KodPocztowyConfiguration : IEntityTypeConfiguration<KodPocztowy>
    {
        public void Configure(EntityTypeBuilder<KodPocztowy> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}