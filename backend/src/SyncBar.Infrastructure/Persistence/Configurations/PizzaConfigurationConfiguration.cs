using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PizzaConfigurationConfiguration : IEntityTypeConfiguration<PizzaConfiguration>
{
    public void Configure(EntityTypeBuilder<PizzaConfiguration> builder)
    {
        builder.ToTable("PizzaConfiguration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // 1:1 com Product — sem índice único filtrado (MySQL sem índice parcial confiável em
        // CREATE INDEX); "1 configuração ativa por produto" é garantido pelo handler
        // (get-or-create), mesmo padrão de IFoodProductMapping/ProductComplementGroup.
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_PizzaConfiguration_ProductId");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId)
            .HasConstraintName("FK_PizzaConfiguration_Product").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Sizes).WithOne().HasForeignKey(s => s.PizzaConfigurationId)
            .HasConstraintName("FK_PizzaSize_PizzaConfiguration").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Sizes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Crusts).WithOne().HasForeignKey(c => c.PizzaConfigurationId)
            .HasConstraintName("FK_PizzaCrust_PizzaConfiguration").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Crusts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Edges).WithOne().HasForeignKey(e => e.PizzaConfigurationId)
            .HasConstraintName("FK_PizzaEdge_PizzaConfiguration").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Edges).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.FlavorPrices).WithOne().HasForeignKey(p => p.PizzaConfigurationId)
            .HasConstraintName("FK_PizzaFlavorPrice_PizzaConfiguration").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.FlavorPrices).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
