using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodFinancialEventConfiguration : IEntityTypeConfiguration<IFoodFinancialEvent>
{
    public void Configure(EntityTypeBuilder<IFoodFinancialEvent> builder)
    {
        builder.ToTable("IFoodFinancialEvent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodEventId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Trigger).HasMaxLength(100);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CompetenceDate).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.SettlementExpectedDate).HasColumnType("datetime(6)");
        builder.Property(x => x.ReferenceType).HasMaxLength(30);
        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        // TEXT — payload bruto pode passar de VARCHAR razoável; guardado só pra auditoria.
        builder.Property(x => x.RawPayload).HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado (MySQL sem índice parcial) — dedup garantido pelo handler
        // (ExistsByIFoodEventIdAsync antes de inserir), mesmo padrão de IFoodProductMapping.
        builder.HasIndex(x => new { x.BranchId, x.IFoodEventId }).HasDatabaseName("IX_IFoodFinancialEvent_BranchId_IFoodEventId");
        builder.HasIndex(x => new { x.BranchId, x.CompetenceDate }).HasDatabaseName("IX_IFoodFinancialEvent_BranchId_CompetenceDate");
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId }).HasDatabaseName("IX_IFoodFinancialEvent_ReferenceType_ReferenceId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodFinancialEvent_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
