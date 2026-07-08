using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class SalePaymentConfiguration : IEntityTypeConfiguration<SalePayment>
{
    public void Configure(EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ChangeAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AuthorizationCode).HasColumnType("varchar(100)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.SaleId).HasDatabaseName("IX_SalePayment_SaleId");
        builder.HasIndex(x => x.PaymentMethodId).HasDatabaseName("IX_SalePayment_PaymentMethodId");
        
        builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).HasConstraintName("FK_SalePayment_PaymentMethod").OnDelete(DeleteBehavior.Restrict);
    }
}
