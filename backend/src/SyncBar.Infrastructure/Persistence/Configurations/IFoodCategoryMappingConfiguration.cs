using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodCategoryMappingConfiguration : IEntityTypeConfiguration<IFoodCategoryMapping>
{
    public void Configure(EntityTypeBuilder<IFoodCategoryMapping> builder)
    {
        builder.ToTable("IFoodCategoryMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodCategoryId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por categoria x
        // filial" é garantido pelo handler (get-or-create por CategoryId+BranchId).
        builder.HasIndex(x => new { x.CategoryId, x.BranchId }).HasDatabaseName("IX_IFoodCategoryMapping_CategoryId_BranchId");

        builder.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId)
            .HasConstraintName("FK_IFoodCategoryMapping_Category").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodCategoryMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
