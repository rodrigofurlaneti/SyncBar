using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sale");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.SubtotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ServiceFeeAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.SoldAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_Sale_BranchId");
        builder.HasIndex(x => x.CashSessionId).HasDatabaseName("IX_Sale_CashSessionId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_Sale_EmployeeId");
        builder.HasIndex(x => x.SoldAt).HasDatabaseName("IX_Sale_SoldAt");
        builder.HasIndex(x => x.CustomerOrderId).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Sale_CustomerOrderId");
        builder.HasIndex(x => new { x.BranchId, x.SaleNumber }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Sale_BranchId_SaleNumber");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_Sale_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId).HasConstraintName("FK_Sale_CustomerOrder").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).HasConstraintName("FK_Sale_CashSession").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_Sale_Employee").OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.SaleId).HasConstraintName("FK_SalePayment_Sale").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
