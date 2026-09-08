using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodEventInboxConfiguration : IEntityTypeConfiguration<IfoodEventInbox>
{
    public void Configure(EntityTypeBuilder<IfoodEventInbox> builder)
    {
        builder.ToTable("ifoodeventinbox");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.EventId).HasMaxLength(100).IsRequired();
        builder.Property(row => row.Payload).HasColumnType("longtext").IsRequired();
        builder.Property(row => row.LastError).HasMaxLength(1000);
        builder.HasIndex(row => new { row.CompanyId, row.EventId }).IsUnique();
        builder.HasIndex(row => new { row.ProcessedAtUtc, row.NextAttemptAtUtc });
        builder.HasOne<Company>().WithMany().HasForeignKey(row => row.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
