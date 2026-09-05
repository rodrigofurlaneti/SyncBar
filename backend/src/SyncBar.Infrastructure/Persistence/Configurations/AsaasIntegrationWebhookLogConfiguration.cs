using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AsaasIntegrationWebhookLogConfiguration : IEntityTypeConfiguration<AsaasIntegrationWebhookLog>
{
    public void Configure(EntityTypeBuilder<AsaasIntegrationWebhookLog> builder)
    {
        builder.ToTable("asaasintegrationwebhooklog");

        // Chave Primária
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Relacionamentos e Identificadores
        builder.Property(x => x.CompanyId)
            .IsRequired();

        builder.Property(x => x.BranchId);

        builder.Property(x => x.AsaasEventId)
            .HasMaxLength(100);

        builder.Property(x => x.PaymentId)
            .HasMaxLength(50);

        // Dados do Webhook
        builder.Property(x => x.Event)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(x => x.RequestHeaders)
            .HasColumnType("text");

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // Tamanho máximo para suportar IPv6 completo

        // Status e Auditoria
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(SyncBar.Domain.Enums.WebhookLogStatus.Pending)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnType("datetime(6)");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime(6)");

        builder.Property(x => x.IsActive)
            .HasColumnType("tinyint(1)")
            .HasDefaultValue(true)
            .IsRequired();

        // Índices de Otimização (Baseados nos métodos do seu Repositório)
        builder.HasIndex(x => x.AsaasEventId)
            .HasDatabaseName("IX_AsaasIntegrationWebhookLog_AsaasEventId");

        builder.HasIndex(x => new { x.CompanyId, x.PaymentId })
            .HasDatabaseName("IX_AsaasIntegrationWebhookLog_Company_Payment");

        builder.HasIndex(x => new { x.CompanyId, x.Status })
            .HasDatabaseName("IX_AsaasIntegrationWebhookLog_Company_Status");
    }
}