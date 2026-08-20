using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodComplementGroupMappingConfiguration : IEntityTypeConfiguration<IFoodComplementGroupMapping>
{
    public void Configure(EntityTypeBuilder<IFoodComplementGroupMapping> builder)
    {
        builder.ToTable("IFoodComplementGroupMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodOptionGroupId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por grupo x
        // filial" é garantido pelo handler (get-or-create), mesmo padrão de IFoodProductMapping.
        builder.HasIndex(x => new { x.ComplementGroupId, x.BranchId }).HasDatabaseName("IX_IFoodComplementGroupMapping_ComplementGroupId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodComplementGroupMapping_BranchId");

        builder.HasOne<ComplementGroup>().WithMany().HasForeignKey(x => x.ComplementGroupId)
            .HasConstraintName("FK_IFoodComplementGroupMapping_ComplementGroup").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodComplementGroupMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
