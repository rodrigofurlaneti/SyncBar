using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationOrderEventLogConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationOrderEventLog))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationOrderEventLogTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationordereventlog");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationOrderEventLog.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_CompanyIdAndBranchId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.BranchId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_EventIdOrderIdAndEventType_ShouldBeRequiredWithExpectedMaxLengths()
        {
            var eventId = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.EventId))!;
            var orderId = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.OrderId))!;
            var eventType = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.EventType))!;

            eventId.IsNullable.Should().BeFalse();
            eventId.GetMaxLength().Should().Be(100);

            orderId.IsNullable.Should().BeFalse();
            orderId.GetMaxLength().Should().Be(100);

            eventType.IsNullable.Should().BeFalse();
            eventType.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_RawPayloadAndErrorMessage_ShouldBeOptionalWithExpectedColumnTypes()
        {
            var rawPayload = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.RawPayload))!;
            var errorMessage = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.ErrorMessage))!;

            rawPayload.IsNullable.Should().BeTrue();
            rawPayload.GetColumnType().Should().Be("json");

            errorMessage.IsNullable.Should().BeTrue();
            errorMessage.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_ProcessedSuccessfully_ShouldBeRequiredTinyintWithDefaultFalse()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.ProcessedSuccessfully))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("tinyint(1)");
            property.GetDefaultValue().Should().Be(false);
        }

        [Fact]
        public void Configure_EventCreatedAtUtcAndReceivedAtUtc_ShouldBeRequiredDatetime6()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.EventCreatedAtUtc))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.EventCreatedAtUtc))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.ReceivedAtUtc))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrderEventLog.ReceivedAtUtc))!.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnEventIdAndIndexOnOrderId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "uid_event_id" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(KeetaIntegrationOrderEventLog.EventId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "idx_event_order_id" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationOrderEventLog.OrderId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaEventLog_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrderEventLog.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaEventLog_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrderEventLog.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        // Esta FK é peculiar: aponta pra KeetaIntegrationOrder não pelo Id (PK padrão), mas pela
        // alternate key KeetaOrderId (HasPrincipalKey), casando com OrderId (string) desta entidade
        // — os dois lados usam o id de negócio do Keeta, não o Id interno (long) do agregado.
        [Fact]
        public void Configure_ShouldDefineCascadingForeignKeyToKeetaIntegrationOrderUsingKeetaOrderIdAsPrincipalKey()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            var fk = foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_KeetaEventLog_Order" &&
                fk.PrincipalEntityType.ClrType == typeof(KeetaIntegrationOrder) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrderEventLog.OrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Cascade).Which;

            fk.PrincipalKey.Properties.Single().Name.Should().Be(nameof(KeetaIntegrationOrder.KeetaOrderId));
        }
    }
}
