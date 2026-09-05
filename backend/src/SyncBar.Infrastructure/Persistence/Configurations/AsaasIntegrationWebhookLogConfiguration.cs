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
    internal sealed class AsaasIntegrationWebhookLogConfiguration : IEntityTypeConfiguration<AsaasIntegrationWebhookLog>
    {
        public void Configure(EntityTypeBuilder<AsaasIntegrationWebhookLog> builder)
        {
            builder.ToTable("asaasintegrationwebhooklog");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.PaymentId)
                .HasMaxLength(50);

            builder.Property(x => x.Event)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Payload)
                .HasColumnType("longtext")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(SyncBar.Domain.Enums.WebhookLogStatus.Pending)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnType("text");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.IsActive)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            // Índice composto para busca rápida de eventos repetidos (idempotência)
            builder.HasIndex(x => new { x.PaymentId, x.Event })
                .HasDatabaseName("IX_AsaasIntegrationWebhookLog_Payment_Event");
        }
    }
}
