using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class RevenueTargetConfiguration : IEntityTypeConfiguration<RevenueTarget>
{
    public void Configure(EntityTypeBuilder<RevenueTarget> builder)
    {
        builder.ToTable("RevenueTarget");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.TargetAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.BranchId, x.ReferenceYear, x.ReferenceMonth })
            .IsUnique().HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_RevenueTarget_BranchId_ReferenceYear_ReferenceMonth");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_RevenueTarget_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
