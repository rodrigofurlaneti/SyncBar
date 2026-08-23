using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemPizzaFlavorConfiguration : IEntityTypeConfiguration<OrderItemPizzaFlavor>
{
    public void Configure(EntityTypeBuilder<OrderItemPizzaFlavor> builder)
    {
        builder.ToTable("OrderItemPizzaFlavor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.FractionShare).HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();

        builder.HasIndex(x => x.PizzaFlavorId).HasDatabaseName("IX_OrderItemPizzaFlavor_PizzaFlavorId");

        // FK pra OrderItem é criada por OrderItemConfiguration (HasMany/dono da coleção).
        builder.HasOne<PizzaFlavor>().WithMany().HasForeignKey(x => x.PizzaFlavorId)
            .HasConstraintName("FK_OrderItemPizzaFlavor_PizzaFlavor").OnDelete(DeleteBehavior.Restrict);
    }
}
