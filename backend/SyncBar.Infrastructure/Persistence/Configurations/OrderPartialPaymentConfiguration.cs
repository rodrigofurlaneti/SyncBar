using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderPartialPaymentConfiguration : IEntityTypeConfiguration<OrderPartialPayment>
{
    public void Configure(EntityTypeBuilder<OrderPartialPayment> builder)
    {
        builder.ToTable("OrderPartialPayment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.AuthorizationCode).HasColumnType("varchar(100)");
        builder.Property(x => x.PayerName).HasColumnType("nvarchar(100)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.CustomerOrderId).HasDatabaseName("IX_OrderPartialPayment_CustomerOrderId");
        builder.HasIndex(x => x.CashSessionId).HasDatabaseName("IX_OrderPartialPayment_CashSessionId");

        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId)
            .HasConstraintName("FK_OrderPartialPayment_CustomerOrder").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId)
            .HasConstraintName("FK_OrderPartialPayment_CashSession").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId)
            .HasConstraintName("FK_OrderPartialPayment_PaymentMethod").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_OrderPartialPayment_Employee").OnDelete(DeleteBehavior.Restrict);
    }
}
