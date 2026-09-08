using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodAnalyticsSnapshotConfiguration : IEntityTypeConfiguration<IfoodAnalyticsSnapshot>
{
    public void Configure(EntityTypeBuilder<IfoodAnalyticsSnapshot> builder)
    {
        builder.ToTable("ifoodanalyticssnapshot");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.MerchantId).HasMaxLength(100).IsRequired();
        builder.Property(row => row.ReferenceDate).HasColumnType("date");
        builder.Property(row => row.AggregatesJson).HasColumnType("json").IsRequired();
        builder.Property(row => row.RefreshedAtUtc).HasColumnType("datetime(6)");
        builder.HasIndex(row => new { row.BranchId, row.MerchantId, row.ReferenceDate }).IsUnique();
        builder.HasOne<Branch>().WithMany().HasForeignKey(row => row.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
