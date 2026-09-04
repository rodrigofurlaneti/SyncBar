using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            builder.ToTable("customeraddress");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId)
                .IsRequired();

            builder.Property(x => x.BranchId)
                .IsRequired(false);

            builder.Property(x => x.CustomerId)
                .IsRequired(false);

            builder.Property(x => x.LastOrderId)
                .IsRequired(false);

            builder.Property(x => x.Street)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Number)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Supplement)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Supplement)
                .HasMaxLength(9)
                .IsRequired();

            builder.Property(x => x.LastOrderAt)
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            // Relacionamentos e Chaves Estrangeiras (Foreign Keys)
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerAddress_Company");

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerAddress_Branch");

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerAddress_Customer");

            builder.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.LastOrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CustomerAddress_Order");
        }
    }
}