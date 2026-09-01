using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodOrderConfiguration : IEntityTypeConfiguration<IfoodOrder>
{
    public void Configure(EntityTypeBuilder<IfoodOrder> builder)
    {
        builder.ToTable("IfoodOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IfoodOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayId).HasMaxLength(50);
        builder.Property(x => x.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IfoodOrderType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DeliveredBy).HasMaxLength(30);
        builder.Property(x => x.OrderTiming).HasMaxLength(20).IsRequired().HasDefaultValue("IMMEDIATE");
        builder.Property(x => x.PreparationStartDateTime).HasColumnType("datetime(6)");
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ConfirmDeadlineAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.ConfirmedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.IfoodOrderId).IsUnique().HasDatabaseName("UQ_IfoodOrder_IfoodOrderId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodOrder_BranchId");
        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_IfoodOrder_CustomerOrderId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodOrder_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .HasConstraintName("FK_IfoodOrder_CustomerOrder").OnDelete(DeleteBehavior.Restrict);
    }
}
