using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.DiscountRate).HasColumnType("decimal(5,4)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_Promotion_BranchId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_Promotion_ProductId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_Promotion_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .HasConstraintName("FK_Promotion_Product").OnDelete(DeleteBehavior.Restrict);
    }
}
