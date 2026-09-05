using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class AsaasIntegrationSavedCardConfiguration : IEntityTypeConfiguration<AsaasIntegrationSavedCard>
    {
        public void Configure(EntityTypeBuilder<AsaasIntegrationSavedCard> builder)
        {
            builder.ToTable("asaasintegrationsavedcard");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.CreditCardToken).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CardBrand).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Last4Digits).HasMaxLength(10).IsRequired();

            builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").HasDefaultValue(true).IsRequired();

            // Índices
            builder.HasIndex(x => x.CustomerId)
                .HasDatabaseName("IX_AsaasIntegrationSavedCard_Customer");

            // Relacionamento com Customer
            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .HasConstraintName("FK_AsaasIntegrationSavedCard_Customer")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
