using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Enums;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class AsaasIntegrationWebhookLogConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(AsaasIntegrationWebhookLog))!;

        [Fact]
        public void Configure_ShouldMapToAsaasIntegrationWebhookLogTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("asaasintegrationwebhooklog");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(AsaasIntegrationWebhookLog.Id));
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CompanyId_ShouldBeRequired_BranchId_ShouldBeOptional()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.BranchId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_AsaasEventIdAndPaymentId_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.AsaasEventId))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.PaymentId))!.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_Event_ShouldBeRequiredWithMaxLength50()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.Event))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_Payload_ShouldBeRequiredLongtext()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.Payload))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("longtext");
        }

        [Fact]
        public void Configure_RequestHeadersAndErrorMessage_ShouldBeOptionalText()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.RequestHeaders))!.GetColumnType().Should().Be("text");
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.ErrorMessage))!.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_IpAddress_ShouldHaveMaxLength45()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.IpAddress))!.GetMaxLength().Should().Be(45);
        }

        [Fact]
        public void Configure_Status_ShouldBeRequiredIntEnumWithDefaultPending()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.Status))!;

            property.IsNullable.Should().BeFalse();
            property.ClrType.Should().Be(typeof(WebhookLogStatus));
            property.GetDefaultValue().Should().Be(WebhookLogStatus.Pending);
        }

        [Fact]
        public void Configure_CreatedAtProcessedAtUpdatedAt_ShouldHaveExpectedNullability()
        {
            var createdAt = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.CreatedAt))!;
            var processedAt = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.ProcessedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            processedAt.IsNullable.Should().BeTrue();
            processedAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyintWithDefaultTrue()
        {
            var isActive = EntityType.FindProperty(nameof(AsaasIntegrationWebhookLog.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationWebhookLog_AsaasEventId" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationWebhookLog.AsaasEventId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationWebhookLog_Company_Payment" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(AsaasIntegrationWebhookLog.CompanyId),
                    nameof(AsaasIntegrationWebhookLog.PaymentId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationWebhookLog_Company_Status" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(AsaasIntegrationWebhookLog.CompanyId),
                    nameof(AsaasIntegrationWebhookLog.Status)
                }));
        }

        [Fact]
        public void Configure_ShouldNotDefineForeignKeys()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
