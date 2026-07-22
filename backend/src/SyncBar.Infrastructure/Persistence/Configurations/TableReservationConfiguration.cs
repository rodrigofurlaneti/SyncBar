using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class TableReservationConfiguration : IEntityTypeConfiguration<TableReservation>
{
    public void Configure(EntityTypeBuilder<TableReservation> builder)
    {
        builder.ToTable("TableReservation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.CustomerName).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.CustomerPhone).HasColumnType("varchar(20)");
        builder.Property(x => x.ReservedFor).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.ReservationStatusId).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(500)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.BranchId, x.ReservedFor }).HasDatabaseName("IX_TableReservation_BranchId_ReservedFor");
        builder.HasIndex(x => x.DiningTableId).HasDatabaseName("IX_TableReservation_DiningTableId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_TableReservation_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DiningTable>().WithMany().HasForeignKey(x => x.DiningTableId).HasConstraintName("FK_TableReservation_DiningTable").OnDelete(DeleteBehavior.Restrict);
    }
}
