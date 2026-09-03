using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ShiftClosingConfiguration : IEntityTypeConfiguration<ShiftClosing>
{
    public void Configure(EntityTypeBuilder<ShiftClosing> builder)
    {
        // Nome da tabela em minusculo, seguindo o padrao do MySQL usado no restante do schema.
        builder.ToTable("shiftclosing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TotalOpeningAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalExpectedAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalRealizedAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalDifferenceAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("varchar(500)");

        builder.Property(x => x.PeriodStart).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();

        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_ShiftClosing_BranchId");
        builder.HasIndex(x => x.ShiftClosingStatusId).HasDatabaseName("IX_ShiftClosing_ShiftClosingStatusId");
        builder.HasIndex(x => x.OpenedByEmployeeId).HasDatabaseName("IX_ShiftClosing_OpenedByEmployeeId");
        builder.HasIndex(x => x.ClosedByEmployeeId).HasDatabaseName("IX_ShiftClosing_ClosedByEmployeeId");
        builder.HasIndex(x => x.PeriodStart).HasDatabaseName("IX_ShiftClosing_PeriodStart");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_ShiftClosing_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ShiftClosingStatus>().WithMany().HasForeignKey(x => x.ShiftClosingStatusId).HasConstraintName("FK_ShiftClosing_ShiftClosingStatus").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.OpenedByEmployeeId).HasConstraintName("FK_ShiftClosing_OpenedByEmployee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ClosedByEmployeeId).HasConstraintName("FK_ShiftClosing_ClosedByEmployee").OnDelete(DeleteBehavior.Restrict);
    }
}
