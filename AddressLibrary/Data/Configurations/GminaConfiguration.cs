using AddressLibrary.Models;
using AddressLibrary.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AddressLibrary.Data.Configurations
{
    public class GminaConfiguration : IEntityTypeConfiguration<Gmina>
    {
        public void Configure(EntityTypeBuilder<Gmina> builder)
        {
            builder.SetAllColumnsCaseSensitive();
        }
    }
}