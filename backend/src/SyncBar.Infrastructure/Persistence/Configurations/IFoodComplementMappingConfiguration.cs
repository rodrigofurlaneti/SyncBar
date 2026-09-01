using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodComplementMappingConfiguration : IEntityTypeConfiguration<IfoodComplementMapping>
{
    public void Configure(EntityTypeBuilder<IfoodComplementMapping> builder)
    {
        builder.ToTable("IfoodComplementMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IfoodOptionId).IsRequired();
        builder.Property(x => x.IfoodProductId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por complemento x
        // filial" é garantido pelo handler (get-or-create), mesmo padrão de IfoodProductMapping.
        builder.HasIndex(x => new { x.ComplementId, x.BranchId }).HasDatabaseName("IX_IfoodComplementMapping_ComplementId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodComplementMapping_BranchId");
        builder.HasIndex(x => x.IfoodOptionId).HasDatabaseName("IX_IfoodComplementMapping_IfoodOptionId");

        builder.HasOne<Complement>().WithMany().HasForeignKey(x => x.ComplementId)
            .HasConstraintName("FK_IfoodComplementMapping_Complement").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodComplementMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
