using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodLogisticsDeliveryConfiguration : IEntityTypeConfiguration<IFoodLogisticsDelivery>
{
    public void Configure(EntityTypeBuilder<IFoodLogisticsDelivery> builder)
    {
        builder.ToTable("IFoodLogisticsDelivery");
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

        builder.HasIndex(x => x.IFoodOrderId).IsUnique().HasDatabaseName("UQ_IFoodLogisticsDelivery_IFoodOrderId");
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodLogisticsDelivery_BranchId");

        builder.HasOne<IFoodOrder>().WithMany().HasForeignKey(x => x.IFoodOrderId)
            .HasConstraintName("FK_IFoodLogisticsDelivery_IFoodOrder").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodLogisticsDelivery_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
