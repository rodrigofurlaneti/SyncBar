using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ProductBoostConfiguration : IEntityTypeConfiguration<ProductBoost>
{
    public void Configure(EntityTypeBuilder<ProductBoost> builder)
    {
        builder.ToTable("productboost");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BoostName).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.IncrementalValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        builder.HasOne<Product>().WithMany(x => x.Boosts).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

