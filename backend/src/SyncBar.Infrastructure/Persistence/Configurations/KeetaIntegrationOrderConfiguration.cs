using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationOrderConfiguration : IEntityTypeConfiguration<KeetaIntegrationOrder>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationOrder> builder)
        {
            builder.ToTable("keetaintegrationorder");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();
            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.CustomerOrderId).IsRequired();

            builder.Property(x => x.KeetaOrderId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.DisplayId)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.InternalMerchantId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.KeetaMerchantId).IsRequired();

            builder.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.OrderType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.DeliveredBy)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.OrderAmount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("BRL")
                .IsRequired();

            builder.Property(x => x.RawOrderJson)
                .HasColumnType("json")
                .IsRequired();

            builder.Property(x => x.OrderCreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.ConfirmedAtUtc)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.ReadyForPickupAtUtc)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.ConcludedAtUtc)
                .HasColumnType("datetime(6)");

            // Índices
            builder.HasIndex(x => x.KeetaOrderId)
                .IsUnique()
                .HasDatabaseName("uid_keeta_order_id");

            builder.HasIndex(x => x.KeetaMerchantId)
                .HasDatabaseName("idx_order_merchant");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaOrder_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaOrder_Branch")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .HasConstraintName("FK_KeetaOrder_Customer")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.CustomerOrderId)
                .HasConstraintName("FK_KeetaOrder_CustomerOrder")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
