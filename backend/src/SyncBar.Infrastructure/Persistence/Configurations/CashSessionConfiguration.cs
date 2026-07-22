using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("CashSession");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.OpeningAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ClosingAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExpectedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DifferenceAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OpenedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ClosedAt).HasColumnType("datetime2");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CashRegisterId).HasDatabaseName("IX_CashSession_CashRegisterId");
        builder.HasIndex(x => x.CashSessionStatusId).HasDatabaseName("IX_CashSession_CashSessionStatusId");
        builder.HasIndex(x => x.OpenedByEmployeeId).HasDatabaseName("IX_CashSession_OpenedByEmployeeId");
        builder.HasIndex(x => x.ClosedByEmployeeId).HasDatabaseName("IX_CashSession_ClosedByEmployeeId");
        
        builder.HasOne<CashRegister>().WithMany().HasForeignKey(x => x.CashRegisterId).HasConstraintName("FK_CashSession_CashRegister").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSessionStatus>().WithMany().HasForeignKey(x => x.CashSessionStatusId).HasConstraintName("FK_CashSession_CashSessionStatus").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.OpenedByEmployeeId).HasConstraintName("FK_CashSession_OpenedByEmployee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ClosedByEmployeeId).HasConstraintName("FK_CashSession_ClosedByEmployee").OnDelete(DeleteBehavior.Restrict);
    }
}
