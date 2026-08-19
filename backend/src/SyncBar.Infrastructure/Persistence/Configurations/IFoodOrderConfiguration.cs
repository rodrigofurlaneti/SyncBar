using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodOrderConfiguration : IEntityTypeConfiguration<IFoodOrder>
{
    public void Configure(EntityTypeBuilder<IFoodOrder> builder)
    {
        builder.ToTable("IFoodOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayId).HasMaxLength(50);
        builder.Property(x => x.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IFoodOrderType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ConfirmDeadlineAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.ConfirmedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.IFoodOrderId).IsUnique().HasDatabaseName("UQ_IFoodOrder_IFoodOrderId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodOrder_BranchId");
        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_IFoodOrder_CustomerOrderId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodOrder_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .HasConstraintName("FK_IFoodOrder_CustomerOrder").OnDelete(DeleteBehavior.Restrict);
    }
}
