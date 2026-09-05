using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AsaasIntegrationPaymentConfiguration : IEntityTypeConfiguration<AsaasIntegrationPayment>
{
    public void Configure(EntityTypeBuilder<AsaasIntegrationPayment> builder)
    {
        builder.ToTable("asaasintegrationpayment");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.CustomerOrderId).IsRequired();
        builder.Property(x => x.CustomerId);

        builder.Property(x => x.AsaasPaymentId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillingType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Value).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.NetValue).HasPrecision(18, 2);

        builder.Property(x => x.DueDate).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnType("datetime(6)");

        builder.Property(x => x.PixQrCodeBase64).HasColumnType("longtext");
        builder.Property(x => x.PixPayload).HasColumnType("text");

        builder.Property(x => x.InvoiceUrl).HasMaxLength(500);
        builder.Property(x => x.BankSlipUrl).HasMaxLength(500);

        builder.Property(x => x.InstallmentCount).HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreditCardToken).HasMaxLength(150);

        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").HasDefaultValue(true).IsRequired();

        // Índices
        builder.HasIndex(x => x.AsaasPaymentId)
            .IsUnique()
            .HasDatabaseName("UQ_AsaasIntegrationPayment_AsaasPaymentId");

        builder.HasIndex(x => x.BranchId)
            .HasDatabaseName("IX_AsaasIntegrationPayment_Branch");

        builder.HasIndex(x => x.CustomerOrderId)
            .HasDatabaseName("IX_AsaasIntegrationPayment_CustomerOrder");

        builder.HasIndex(x => x.CustomerId)
            .HasDatabaseName("IX_AsaasIntegrationPayment_Customer");

        // Relacionamentos com deleção restrita
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_AsaasIntegrationPayment_Branch")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CustomerOrder>()
            .WithMany()
            .HasForeignKey(x => x.CustomerOrderId)
            .HasConstraintName("FK_AsaasIntegrationPayment_CustomerOrder")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .HasConstraintName("FK_AsaasIntegrationPayment_Customer")
            .OnDelete(DeleteBehavior.Restrict);
    }
}