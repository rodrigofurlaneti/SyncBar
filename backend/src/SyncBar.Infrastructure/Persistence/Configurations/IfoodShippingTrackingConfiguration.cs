using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodShippingTrackingConfiguration : IEntityTypeConfiguration<IfoodShippingTracking>
{
    public void Configure(EntityTypeBuilder<IfoodShippingTracking> builder)
    {
        builder.ToTable("ifoodshippingtracking");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.OrderId).HasMaxLength(100).IsRequired();
        builder.HasIndex(row => new { row.CompanyId, row.OrderId }).IsUnique();
        builder.HasIndex(row => new { row.IsActive, row.NextPollAtUtc });
        builder.HasOne<Branch>().WithMany().HasForeignKey(row => row.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
