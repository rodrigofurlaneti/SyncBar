using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PizzaEdgeConfiguration : IEntityTypeConfiguration<PizzaEdge>
{
    public void Configure(EntityTypeBuilder<PizzaEdge> builder)
    {
        builder.ToTable("PizzaEdge");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.ExtraPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.PizzaConfigurationId).HasDatabaseName("IX_PizzaEdge_PizzaConfigurationId");

        // FK pra PizzaConfiguration é criada por PizzaConfigurationConfiguration (HasMany/dono da coleção).
    }
}
