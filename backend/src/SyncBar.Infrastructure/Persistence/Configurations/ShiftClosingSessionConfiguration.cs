using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ShiftClosingSessionConfiguration : IEntityTypeConfiguration<ShiftClosingSession>
{
    public void Configure(EntityTypeBuilder<ShiftClosingSession> builder)
    {
        builder.ToTable("shiftclosingsession");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();

        // Um mesmo CashSession nao pode ser consolidado duas vezes num fechamento de turno.
        builder.HasIndex(x => new { x.ShiftClosingId, x.CashSessionId })
            .IsUnique()
            .HasDatabaseName("UQ_ShiftClosingSession_ShiftClosingId_CashSessionId");

        builder.HasIndex(x => x.CashSessionId).HasDatabaseName("IX_ShiftClosingSession_CashSessionId");

        builder.HasOne<ShiftClosing>().WithMany().HasForeignKey(x => x.ShiftClosingId).HasConstraintName("FK_ShiftClosingSession_ShiftClosing").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).HasConstraintName("FK_ShiftClosingSession_CashSession").OnDelete(DeleteBehavior.Restrict);
    }
}
