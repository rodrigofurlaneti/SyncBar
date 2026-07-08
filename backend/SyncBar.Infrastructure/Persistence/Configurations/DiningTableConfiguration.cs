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
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.TableStatusId).HasDatabaseName("IX_DiningTable_TableStatusId");
        builder.HasIndex(x => new { x.BranchId, x.Number }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_DiningTable_BranchId_Number");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_DiningTable_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableStatus>().WithMany().HasForeignKey(x => x.TableStatusId).HasConstraintName("FK_DiningTable_TableStatus").OnDelete(DeleteBehavior.Restrict);
    }
}
