using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodSettlementConfiguration : IEntityTypeConfiguration<IFoodSettlement>
{
    public void Configure(EntityTypeBuilder<IFoodSettlement> builder)
    {
        builder.ToTable("IFoodSettlement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.IFoodSettlementId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Product).HasMaxLength(50);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnType("datetime(6)");
        builder.Property(x => x.BankCode).HasMaxLength(20);
        builder.Property(x => x.BankAgency).HasMaxLength(20);
        builder.Property(x => x.BankAccount).HasMaxLength(30);
        builder.Property(x => x.RawPayload).HasColumnType("text").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => new { x.BranchId, x.IFoodSettlementId }).HasDatabaseName("IX_IFoodSettlement_BranchId_IFoodSettlementId");
        builder.HasIndex(x => new { x.BranchId, x.PaymentDate }).HasDatabaseName("IX_IFoodSettlement_BranchId_PaymentDate");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodSettlement_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
