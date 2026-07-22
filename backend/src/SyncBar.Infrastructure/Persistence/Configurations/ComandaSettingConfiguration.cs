using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ComandaSettingConfiguration : IEntityTypeConfiguration<ComandaSetting>
{
    public void Configure(EntityTypeBuilder<ComandaSetting> builder)
    {
        builder.ToTable("ComandaSetting");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.DefaultLimitAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.BranchId).IsUnique().HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_ComandaSetting_BranchId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_ComandaSetting_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
