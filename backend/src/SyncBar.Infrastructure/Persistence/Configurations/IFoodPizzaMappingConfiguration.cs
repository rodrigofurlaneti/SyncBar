using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodPizzaMappingConfiguration : IEntityTypeConfiguration<IfoodPizzaMapping>
{
    public void Configure(EntityTypeBuilder<IfoodPizzaMapping> builder)
    {
        builder.ToTable("IfoodPizzaMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IfoodPizzaId).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento ativo por
        // configuração x filial" é garantido pelo handler (get-or-create), mesmo padrão de
        // IfoodComplementGroupMapping/IfoodProductMapping.
        builder.HasIndex(x => new { x.PizzaConfigurationId, x.BranchId }).HasDatabaseName("IX_IfoodPizzaMapping_PizzaConfigurationId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodPizzaMapping_BranchId");

        builder.HasOne<PizzaConfiguration>().WithMany().HasForeignKey(x => x.PizzaConfigurationId)
            .HasConstraintName("FK_IfoodPizzaMapping_PizzaConfiguration").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodPizzaMapping_Branch").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Elements).WithOne().HasForeignKey(e => e.IfoodPizzaMappingId)
            .HasConstraintName("FK_IfoodPizzaElementMapping_IfoodPizzaMapping").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Elements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
