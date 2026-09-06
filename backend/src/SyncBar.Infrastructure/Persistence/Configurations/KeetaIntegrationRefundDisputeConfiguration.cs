using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationRefundDisputeConfiguration : IEntityTypeConfiguration<KeetaIntegrationRefundDispute>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationRefundDispute> builder)
        {
            builder.ToTable("keetaintegrationrefunddispute");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();

            builder.Property(x => x.OrderId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.AfterSaleOrderId).IsRequired();

            builder.Property(x => x.RefundAmount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("BRL")
                .IsRequired();

            builder.Property(x => x.ApplyReason)
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.ResolutionStatus)
                .HasMaxLength(50)
                .HasDefaultValue("PENDING")
                .IsRequired();

            builder.Property(x => x.DenialReasonCode)
                .HasMaxLength(50);

            builder.Property(x => x.DenialReasonText)
                .HasColumnType("text");

            builder.Property(x => x.ReceivedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.ResolvedAtUtc)
                .HasColumnType("datetime(6)");

            // Índices
            builder.HasIndex(x => x.AfterSaleOrderId)
                .IsUnique()
                .HasDatabaseName("uid_after_sale_order");

            builder.HasIndex(x => x.OrderId)
                .HasDatabaseName("idx_refund_order_id");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaRefund_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaRefund_Branch")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<KeetaIntegrationOrder>()
                .WithMany()
                .HasPrincipalKey(x => x.KeetaOrderId)
                .HasForeignKey(x => x.OrderId)
                .HasConstraintName("FK_KeetaRefund_Order")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
