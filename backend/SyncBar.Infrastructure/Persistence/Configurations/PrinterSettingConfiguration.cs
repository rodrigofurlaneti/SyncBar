using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PrinterSettingConfiguration : IEntityTypeConfiguration<PrinterSetting>
{
    public void Configure(EntityTypeBuilder<PrinterSetting> builder)
    {
        builder.ToTable("PrinterSetting");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.BranchId).IsUnique().HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_PrinterSetting_BranchId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_PrinterSetting_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
