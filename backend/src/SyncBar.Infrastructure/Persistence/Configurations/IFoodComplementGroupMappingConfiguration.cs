using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodComplementGroupMappingConfiguration : IEntityTypeConfiguration<IfoodComplementGroupMapping>
{
    public void Configure(EntityTypeBuilder<IfoodComplementGroupMapping> builder)
    {
        builder.ToTable("IfoodComplementGroupMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IfoodOptionGroupId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por grupo x
        // filial" é garantido pelo handler (get-or-create), mesmo padrão de IfoodProductMapping.
        builder.HasIndex(x => new { x.ComplementGroupId, x.BranchId }).HasDatabaseName("IX_IfoodComplementGroupMapping_ComplementGroupId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodComplementGroupMapping_BranchId");

        builder.HasOne<ComplementGroup>().WithMany().HasForeignKey(x => x.ComplementGroupId)
            .HasConstraintName("FK_IfoodComplementGroupMapping_ComplementGroup").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodComplementGroupMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
