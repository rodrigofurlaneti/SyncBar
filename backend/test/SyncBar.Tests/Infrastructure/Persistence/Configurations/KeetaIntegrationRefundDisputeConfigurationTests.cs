using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationRefundDisputeConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationRefundDispute))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationRefundDisputeTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationrefunddispute");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationRefundDispute.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_CompanyIdBranchIdAndAfterSaleOrderId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.AfterSaleOrderId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_OrderId_ShouldBeRequiredWithMaxLength100()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.OrderId))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_RefundAmount_ShouldBeRequiredDecimal10_2()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.RefundAmount))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("decimal(10,2)");
        }

        [Fact]
        public void Configure_Currency_ShouldBeRequiredWithMaxLength3AndDefaultBrl()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.Currency))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(3);
            property.GetDefaultValue().Should().Be("BRL");
        }

        [Fact]
        public void Configure_ApplyReason_ShouldBeRequiredText()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.ApplyReason))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_ResolutionStatus_ShouldBeRequiredWithMaxLength50AndDefaultPending()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.ResolutionStatus))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
            property.GetDefaultValue().Should().Be("PENDING");
        }

        [Fact]
        public void Configure_OptionalDenialReasonFields_ShouldHaveExpectedColumnTypes()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.DenialReasonCode))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.DenialReasonText))!.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_ReceivedAtUtc_ShouldBeRequired_ResolvedAtUtc_ShouldBeNullable()
        {
            var receivedAtUtc = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.ReceivedAtUtc))!;
            var resolvedAtUtc = EntityType.FindProperty(nameof(KeetaIntegrationRefundDispute.ResolvedAtUtc))!;

            receivedAtUtc.IsNullable.Should().BeFalse();
            receivedAtUtc.GetColumnType().Should().Be("datetime(6)");

            resolvedAtUtc.IsNullable.Should().BeTrue();
            resolvedAtUtc.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnAfterSaleOrderIdAndIndexOnOrderId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "uid_after_sale_order" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(KeetaIntegrationRefundDispute.AfterSaleOrderId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "idx_refund_order_id" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationRefundDispute.OrderId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaRefund_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationRefundDispute.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaRefund_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationRefundDispute.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configure_ShouldDefineCascadingForeignKeyToKeetaIntegrationOrderUsingKeetaOrderIdAsPrincipalKey()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            var fk = foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_KeetaRefund_Order" &&
                fk.PrincipalEntityType.ClrType == typeof(KeetaIntegrationOrder) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationRefundDispute.OrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Cascade).Which;

            fk.PrincipalKey.Properties.Single().Name.Should().Be(nameof(KeetaIntegrationOrder.KeetaOrderId));
        }
    }
}
