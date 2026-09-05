using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AsaasIntegrationCustomerConfiguration : IEntityTypeConfiguration<AsaasIntegrationCustomer>
{
    public void Configure(EntityTypeBuilder<AsaasIntegrationCustomer> builder)
    {
        builder.ToTable("asaasintegrationcustomer");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.AsaasCustomerId).HasMaxLength(50).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").HasDefaultValue(true).IsRequired();

        // Índices e Constraints
        builder.HasIndex(x => new { x.CustomerId, x.CompanyId })
            .IsUnique()
            .HasDatabaseName("UQ_AsaasIntegrationCustomer_Customer_Company");

        builder.HasIndex(x => x.AsaasCustomerId)
            .HasDatabaseName("IX_AsaasIntegrationCustomer_AsaasCustomerId");

        builder.HasIndex(x => x.CompanyId)
            .HasDatabaseName("IX_AsaasIntegrationCustomer_Company");

        // Relacionamentos com deleção restrita
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .HasConstraintName("FK_AsaasIntegrationCustomer_Customer")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_AsaasIntegrationCustomer_Company")
            .OnDelete(DeleteBehavior.Restrict);
    }
}