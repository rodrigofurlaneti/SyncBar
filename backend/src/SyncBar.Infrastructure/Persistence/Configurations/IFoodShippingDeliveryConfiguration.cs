using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodShippingDeliveryConfiguration : IEntityTypeConfiguration<IfoodShippingDelivery>
{
    public void Configure(EntityTypeBuilder<IfoodShippingDelivery> builder)
    {
        builder.ToTable("IfoodShippingDelivery");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderReference).HasMaxLength(150);
        builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerPhoneAreaCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.CustomerPhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(15).IsRequired();
        builder.Property(x => x.StreetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StreetNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Complement).HasMaxLength(100);
        builder.Property(x => x.Neighborhood).HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.State).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.Property(x => x.MerchantFee).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.QuoteId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IfoodDeliveryId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TrackingUrl).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(300);
        builder.Property(x => x.RequestedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.CancelledAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IfoodShippingDelivery_BranchId");
        builder.HasIndex(x => x.IfoodDeliveryId).IsUnique().HasDatabaseName("UQ_IfoodShippingDelivery_IfoodDeliveryId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodShippingDelivery_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
