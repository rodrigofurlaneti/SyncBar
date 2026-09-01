using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodLogisticsDeliveryConfiguration : IEntityTypeConfiguration<IfoodLogisticsDelivery>
{
    public void Configure(EntityTypeBuilder<IfoodLogisticsDelivery> builder)
    {
        builder.ToTable("IfoodLogisticsDelivery");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.DriverName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DriverPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DriverVehicleType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AssignedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.GoingToOriginAt).HasColumnType("datetime(6)");
        builder.Property(x => x.ArrivedAtOriginAt).HasColumnType("datetime(6)");
        builder.Property(x => x.DispatchedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.ArrivedAtDestinationAt).HasColumnType("datetime(6)");
        builder.Property(x => x.DeliveryCodeVerifiedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.IfoodOrderId).IsUnique().HasDatabaseName("UQ_IfoodLogisticsDelivery_IfoodOrderId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodLogisticsDelivery_BranchId");

        builder.HasOne<IfoodOrder>().WithMany().HasForeignKey(x => x.IfoodOrderId)
            .HasConstraintName("FK_IfoodLogisticsDelivery_IfoodOrder").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodLogisticsDelivery_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
