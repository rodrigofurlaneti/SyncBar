using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodProductMappingConfiguration : IEntityTypeConfiguration<IFoodProductMapping>
{
    public void Configure(EntityTypeBuilder<IFoodProductMapping> builder)
    {
        builder.ToTable("IFoodProductMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodItemId).IsRequired();
        builder.Property(x => x.IFoodProductId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por produto x
        // filial" é garantido pelo handler (get-or-create por ProductId+BranchId).
        builder.HasIndex(x => new { x.ProductId, x.BranchId }).HasDatabaseName("IX_IFoodProductMapping_ProductId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodProductMapping_BranchId");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .HasConstraintName("FK_IFoodProductMapping_Product").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodProductMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
