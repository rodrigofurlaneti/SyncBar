using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class AsaasIntegrationSettingConfiguration : IEntityTypeConfiguration<AsaasIntegrationSetting>
    {
        public void Configure(EntityTypeBuilder<AsaasIntegrationSetting> builder)
        {
            builder.ToTable("asaasintegrationsetting");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId);

            builder.Property(x => x.Environment)
                .HasMaxLength(20)
                .HasDefaultValue("Sandbox")
                .IsRequired();

            builder.Property(x => x.ApiKeyEncrypted)
                .HasColumnType("varchar(1000)")
                .IsRequired();

            builder.Property(x => x.WebhookSecretEncrypted)
                .HasColumnType("varchar(500)");

            builder.Property(x => x.WalletId)
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.IsActive)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            // Índices
            builder.HasIndex(x => x.CompanyId)
                .HasDatabaseName("IX_AsaasIntegrationSetting_Company");

            builder.HasIndex(x => x.BranchId)
                .HasDatabaseName("IX_AsaasIntegrationSetting_Branch");

            // Relacionamentos com deleção restrita
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_AsaasIntegrationSetting_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_AsaasIntegrationSetting_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }   
}
