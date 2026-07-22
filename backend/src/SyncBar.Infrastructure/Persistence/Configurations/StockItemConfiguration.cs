using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.CurrentQuantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.MinimumQuantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.MaximumQuantity).HasColumnType("decimal(18,3)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_StockItem_ProductId");
        builder.HasIndex(x => new { x.BranchId, x.ProductId }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_StockItem_BranchId_ProductId");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_StockItem_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_StockItem_Product").OnDelete(DeleteBehavior.Restrict);
    }
}
