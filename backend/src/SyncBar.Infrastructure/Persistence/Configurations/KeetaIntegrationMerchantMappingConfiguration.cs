using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationMerchantMappingConfiguration : IEntityTypeConfiguration<KeetaIntegrationMerchantMapping>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationMerchantMapping> builder)
        {
            builder.ToTable("keetaintegrationmerchantmapping");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();

            builder.Property(x => x.InternalMerchantId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.KeetaMerchantId).IsRequired();

            builder.Property(x => x.StoreName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.TimeZone)
                .HasMaxLength(50);

            builder.Property(x => x.IsAuthorized)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.IsOnboarded)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.LastMenuSyncAtUtc)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.MenuBaseUrl)
                .HasMaxLength(255);

            builder.Property(x => x.WebhookUrl)
                .HasMaxLength(255);

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            // Índices Multi-tenant
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.KeetaMerchantId })
                .IsUnique()
                .HasDatabaseName("uid_keeta_merchant");

            builder.HasIndex(x => x.InternalMerchantId)
                .HasDatabaseName("idx_internal_merchant");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaMapping_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaMapping_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
