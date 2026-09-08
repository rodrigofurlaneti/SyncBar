using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CashSessionPaymentReconciliationConfiguration : IEntityTypeConfiguration<CashSessionPaymentReconciliation>
{
    public void Configure(EntityTypeBuilder<CashSessionPaymentReconciliation> builder)
    {
        builder.ToTable("cashsessionpaymentreconciliation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ExpectedAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CountedAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.DifferenceAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();

        // No maximo uma linha de conferencia ativa por forma de pagamento por sessao de caixa.
        builder.HasIndex(x => new { x.CashSessionId, x.PaymentMethodId })
            .IsUnique()
            .HasDatabaseName("UQ_CashSessionPaymentReconciliation_CashSessionId_PaymentMethodId");

        builder.HasIndex(x => x.PaymentMethodId).HasDatabaseName("IX_CashSessionPaymentReconciliation_PaymentMethodId");

        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).HasConstraintName("FK_CashSessionPaymentReconciliation_CashSession").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).HasConstraintName("FK_CashSessionPaymentReconciliation_PaymentMethod").OnDelete(DeleteBehavior.Restrict);
    }
}
