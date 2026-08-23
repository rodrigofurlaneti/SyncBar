using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodPizzaElementMappingConfiguration : IEntityTypeConfiguration<IFoodPizzaElementMapping>
{
    public void Configure(EntityTypeBuilder<IFoodPizzaElementMapping> builder)
    {
        builder.ToTable("IFoodPizzaElementMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Kind usa as constantes de IFoodPizzaElementKind (Size=1, Crust=2, Edge=3, Topping=4) —
        // sem tabela lookup, mesmo espírito de ComplementGroup.ComplementGroupTypeId.
        builder.Property(x => x.Kind).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.LocalId).IsRequired();
        builder.Property(x => x.IFoodElementId).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => new { x.IFoodPizzaMappingId, x.Kind, x.LocalId })
            .HasDatabaseName("IX_IFoodPizzaElementMapping_MappingId_Kind_LocalId");

        // FK pra IFoodPizzaMapping é criada por IFoodPizzaMappingConfiguration (HasMany/dono da coleção).
    }
}
