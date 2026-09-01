using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodPizzaElementMappingConfiguration : IEntityTypeConfiguration<IfoodPizzaElementMapping>
{
    public void Configure(EntityTypeBuilder<IfoodPizzaElementMapping> builder)
    {
        builder.ToTable("IfoodPizzaElementMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Kind usa as constantes de IfoodPizzaElementKind (Size=1, Crust=2, Edge=3, Topping=4) —
        // sem tabela lookup, mesmo espírito de ComplementGroup.ComplementGroupTypeId.
        builder.Property(x => x.Kind).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.LocalId).IsRequired();
        builder.Property(x => x.IfoodElementId).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => new { x.IfoodPizzaMappingId, x.Kind, x.LocalId })
            .HasDatabaseName("IX_IfoodPizzaElementMapping_MappingId_Kind_LocalId");

        // FK pra IfoodPizzaMapping é criada por IfoodPizzaMappingConfiguration (HasMany/dono da coleção).
    }
}
