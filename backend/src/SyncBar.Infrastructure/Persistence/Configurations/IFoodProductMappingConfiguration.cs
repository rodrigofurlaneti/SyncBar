using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodProductMappingConfiguration : IEntityTypeConfiguration<IfoodProductMapping>
{
    public void Configure(EntityTypeBuilder<IfoodProductMapping> builder)
    {
        builder.ToTable("IfoodProductMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IfoodItemId).IsRequired();
        builder.Property(x => x.IfoodProductId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por produto x
        // filial" é garantido pelo handler (get-or-create por ProductId+BranchId).
        builder.HasIndex(x => new { x.ProductId, x.BranchId }).HasDatabaseName("IX_IfoodProductMapping_ProductId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodProductMapping_BranchId");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .HasConstraintName("FK_IfoodProductMapping_Product").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodProductMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
