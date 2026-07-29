using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class ProductStockConfiguration : IEntityTypeConfiguration<ProductStock>
    {
        public void Configure(EntityTypeBuilder<ProductStock> builder)
        {
            builder.HasKey(x => x.ProductId);

            builder.Property(x => x.CurrentBalance)
                   .HasPrecision(18, 4);

            builder.Property(x => x.RowVersion)
                   .IsRowVersion();
        }
    }
}