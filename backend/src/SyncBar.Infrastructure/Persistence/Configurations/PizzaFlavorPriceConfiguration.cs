using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PizzaFlavorPriceConfiguration : IEntityTypeConfiguration<PizzaFlavorPrice>
{
    public void Configure(EntityTypeBuilder<PizzaFlavorPrice> builder)
    {
        builder.ToTable("PizzaFlavorPrice");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Price).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.PizzaConfigurationId).HasDatabaseName("IX_PizzaFlavorPrice_PizzaConfigurationId");
        builder.HasIndex(x => x.PizzaFlavorId).HasDatabaseName("IX_PizzaFlavorPrice_PizzaFlavorId");
        // Sem índice único filtrado (MySQL sem índice parcial) — "1 preço ativo por sabor x
        // tamanho" é garantido pelo handler (PizzaConfiguration.SetFlavorPrice já faz upsert
        // in-memory antes de persistir), mesmo padrão de IfoodComplementGroupMapping.
        builder.HasIndex(x => new { x.PizzaFlavorId, x.PizzaSizeId }).HasDatabaseName("IX_PizzaFlavorPrice_PizzaFlavorId_PizzaSizeId");

        // FK pra PizzaConfiguration é criada por PizzaConfigurationConfiguration (HasMany/dono da coleção).
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(x => x.PizzaFlavorId)
            .HasConstraintName("FK_PizzaFlavorPrice_PizzaFlavor").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(x => x.PizzaSizeId)
            .HasConstraintName("FK_PizzaFlavorPrice_PizzaSize").OnDelete(DeleteBehavior.Restrict);
    }
}
