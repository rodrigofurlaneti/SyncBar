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
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(300)");
        builder.Property(x => x.SentToKitchenAt).HasColumnType("datetime2");
        builder.Property(x => x.DeliveredAt).HasColumnType("datetime2");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_OrderItem_CustomerOrderId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_OrderItem_ProductId");
        builder.HasIndex(x => x.OrderItemStatusId).HasDatabaseName("IX_OrderItem_OrderItemStatusId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_OrderItem_EmployeeId");
        
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_OrderItem_Product").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItemStatus>().WithMany().HasForeignKey(x => x.OrderItemStatusId).HasConstraintName("FK_OrderItem_OrderItemStatus").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_OrderItem_Employee").OnDelete(DeleteBehavior.Restrict);
    }
}
