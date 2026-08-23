using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(300)");
        builder.Property(x => x.SentToKitchenAt).HasColumnType("datetime(6)");
        builder.Property(x => x.DeliveredAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_OrderItem_CustomerOrderId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_OrderItem_ProductId");
        builder.HasIndex(x => x.OrderItemStatusId).HasDatabaseName("IX_OrderItem_OrderItemStatusId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_OrderItem_EmployeeId");
        // Fase 17 — só preenchidos para itens de pizza (ver OrderItem.CreatePizza).
        builder.HasIndex(x => x.PizzaSizeId).HasDatabaseName("IX_OrderItem_PizzaSizeId");

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_OrderItem_Product").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItemStatus>().WithMany().HasForeignKey(x => x.OrderItemStatusId).HasConstraintName("FK_OrderItem_OrderItemStatus").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_OrderItem_Employee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaSize>().WithMany().HasForeignKey(x => x.PizzaSizeId).HasConstraintName("FK_OrderItem_PizzaSize").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaCrust>().WithMany().HasForeignKey(x => x.PizzaCrustId).HasConstraintName("FK_OrderItem_PizzaCrust").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PizzaEdge>().WithMany().HasForeignKey(x => x.PizzaEdgeId).HasConstraintName("FK_OrderItem_PizzaEdge").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Complements).WithOne().HasForeignKey(c => c.OrderItemId)
            .HasConstraintName("FK_OrderItemComplement_OrderItem").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Complements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.PizzaFlavors).WithOne().HasForeignKey(f => f.OrderItemId)
            .HasConstraintName("FK_OrderItemPizzaFlavor_OrderItem").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.PizzaFlavors).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
