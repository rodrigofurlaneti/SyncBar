using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PizzaSizeConfiguration : IEntityTypeConfiguration<PizzaSize>
{
    public void Configure(EntityTypeBuilder<PizzaSize> builder)
    {
        builder.ToTable("PizzaSize");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Slices).HasColumnType("int");
        builder.Property(x => x.AcceptedFractions).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.PizzaConfigurationId).HasDatabaseName("IX_PizzaSize_PizzaConfigurationId");

        // FK pra PizzaConfiguration é criada por PizzaConfigurationConfiguration (HasMany/dono da coleção).
    }
}
