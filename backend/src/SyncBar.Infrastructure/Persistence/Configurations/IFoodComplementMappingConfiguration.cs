using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodComplementMappingConfiguration : IEntityTypeConfiguration<IFoodComplementMapping>
{
    public void Configure(EntityTypeBuilder<IFoodComplementMapping> builder)
    {
        builder.ToTable("IFoodComplementMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodOptionId).IsRequired();
        builder.Property(x => x.IFoodProductId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento por complemento x
        // filial" é garantido pelo handler (get-or-create), mesmo padrão de IFoodProductMapping.
        builder.HasIndex(x => new { x.ComplementId, x.BranchId }).HasDatabaseName("IX_IFoodComplementMapping_ComplementId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodComplementMapping_BranchId");
        builder.HasIndex(x => x.IFoodOptionId).HasDatabaseName("IX_IFoodComplementMapping_IFoodOptionId");

        builder.HasOne<Complement>().WithMany().HasForeignKey(x => x.ComplementId)
            .HasConstraintName("FK_IFoodComplementMapping_Complement").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodComplementMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
