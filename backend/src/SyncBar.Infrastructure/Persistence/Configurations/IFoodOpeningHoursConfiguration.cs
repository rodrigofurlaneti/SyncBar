using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodOpeningHoursConfiguration : IEntityTypeConfiguration<IfoodOpeningHours>
{
    public void Configure(EntityTypeBuilder<IfoodOpeningHours> builder)
    {
        builder.ToTable("IfoodOpeningHours");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.Start).HasColumnType("time(0)").IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => new { x.BranchId, x.DayOfWeek }).HasDatabaseName("IX_IfoodOpeningHours_BranchId_DayOfWeek");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IfoodOpeningHours_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
