using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("DiningTable");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.QrToken);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(x => x.TableStatusId).HasDatabaseName("IX_DiningTable_TableStatusId");
        builder.HasIndex(x => x.QrToken).IsUnique().HasDatabaseName("UQ_DiningTable_QrToken");
        builder.HasIndex(x => new { x.BranchId, x.Number }).HasDatabaseName("IX_DiningTable_BranchId_Number");
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_DiningTable_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableStatus>().WithMany().HasForeignKey(x => x.TableStatusId).HasConstraintName("FK_DiningTable_TableStatus").OnDelete(DeleteBehavior.Restrict);
    }
}
