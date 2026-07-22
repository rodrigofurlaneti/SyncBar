using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("CustomerOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.OpenedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ClosedAt).HasColumnType("datetime2");
        builder.Property(x => x.SubtotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ServiceFeeAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreditLimitAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OrderTypeId).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.CustomerName).HasColumnType("nvarchar(150)");
        builder.Property(x => x.CustomerPhone).HasColumnType("varchar(20)");
        builder.Property(x => x.DeliveryAddress).HasColumnType("nvarchar(300)");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).HasConstraintName("FK_CustomerOrder_Customer").OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Notes).HasColumnType("nvarchar(500)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_CustomerOrder_BranchId");
        builder.HasIndex(x => x.DiningTableId).HasDatabaseName("IX_CustomerOrder_DiningTableId");
        builder.HasIndex(x => x.ComandaId).HasDatabaseName("IX_CustomerOrder_ComandaId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_CustomerOrder_EmployeeId");
        builder.HasIndex(x => x.OrderStatusId).HasDatabaseName("IX_CustomerOrder_OrderStatusId");
        builder.HasIndex(x => x.OpenedAt).HasDatabaseName("IX_CustomerOrder_OpenedAt");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("IX_CustomerOrder_CustomerId");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_CustomerOrder_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiningTable>().WithMany().HasForeignKey(x => x.DiningTableId).HasConstraintName("FK_CustomerOrder_DiningTable").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Comanda>().WithMany().HasForeignKey(x => x.ComandaId).HasConstraintName("FK_CustomerOrder_Comanda").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_CustomerOrder_Employee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderStatus>().WithMany().HasForeignKey(x => x.OrderStatusId).HasConstraintName("FK_CustomerOrder_OrderStatus").OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.CustomerOrderId).HasConstraintName("FK_OrderItem_CustomerOrder").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
