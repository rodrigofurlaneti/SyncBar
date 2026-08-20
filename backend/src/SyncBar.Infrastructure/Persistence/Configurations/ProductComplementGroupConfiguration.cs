using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ProductComplementGroupConfiguration : IEntityTypeConfiguration<ProductComplementGroup>
{
    public void Configure(EntityTypeBuilder<ProductComplementGroup> builder)
    {
        builder.ToTable("ProductComplementGroup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_ProductComplementGroup_ProductId");
        builder.HasIndex(x => x.ComplementGroupId).HasDatabaseName("IX_ProductComplementGroup_ComplementGroupId");
        builder.HasIndex(x => new { x.ProductId, x.ComplementGroupId }).IsUnique()
            .HasFilter("[IsActive] = 1").HasDatabaseName("UQ_ProductComplementGroup_ProductId_ComplementGroupId");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .HasConstraintName("FK_ProductComplementGroup_Product").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ComplementGroup>().WithMany().HasForeignKey(x => x.ComplementGroupId)
            .HasConstraintName("FK_ProductComplementGroup_ComplementGroup").OnDelete(DeleteBehavior.Restrict);
    }
}
