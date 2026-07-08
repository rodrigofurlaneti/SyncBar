using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DocumentNumber).HasColumnType("varchar(50)");
        builder.Property(x => x.MovedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(300)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.StockItemId).HasDatabaseName("IX_StockMovement_StockItemId");
        builder.HasIndex(x => x.StockMovementTypeId).HasDatabaseName("IX_StockMovement_StockMovementTypeId");
        builder.HasIndex(x => x.PurchaseItemId).HasDatabaseName("IX_StockMovement_PurchaseItemId");
        builder.HasIndex(x => x.OrderItemId).HasDatabaseName("IX_StockMovement_OrderItemId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_StockMovement_EmployeeId");
        builder.HasIndex(x => x.MovedAt).HasDatabaseName("IX_StockMovement_MovedAt");
        
        builder.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).HasConstraintName("FK_StockMovement_StockItem").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovementType>().WithMany().HasForeignKey(x => x.StockMovementTypeId).HasConstraintName("FK_StockMovement_StockMovementType").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseItem>().WithMany().HasForeignKey(x => x.PurchaseItemId).HasConstraintName("FK_StockMovement_PurchaseItem").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).HasConstraintName("FK_StockMovement_OrderItem").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_StockMovement_Employee").OnDelete(DeleteBehavior.Restrict);
    }
}
