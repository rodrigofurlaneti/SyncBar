using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodPizzaMappingConfiguration : IEntityTypeConfiguration<IFoodPizzaMapping>
{
    public void Configure(EntityTypeBuilder<IFoodPizzaMapping> builder)
    {
        builder.ToTable("IFoodPizzaMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodPizzaId).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — "1 mapeamento ativo por
        // configuração x filial" é garantido pelo handler (get-or-create), mesmo padrão de
        // IFoodComplementGroupMapping/IFoodProductMapping.
        builder.HasIndex(x => new { x.PizzaConfigurationId, x.BranchId }).HasDatabaseName("IX_IFoodPizzaMapping_PizzaConfigurationId_BranchId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodPizzaMapping_BranchId");

        builder.HasOne<PizzaConfiguration>().WithMany().HasForeignKey(x => x.PizzaConfigurationId)
            .HasConstraintName("FK_IFoodPizzaMapping_PizzaConfiguration").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodPizzaMapping_Branch").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Elements).WithOne().HasForeignKey(e => e.IFoodPizzaMappingId)
            .HasConstraintName("FK_IFoodPizzaElementMapping_IFoodPizzaMapping").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Elements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
