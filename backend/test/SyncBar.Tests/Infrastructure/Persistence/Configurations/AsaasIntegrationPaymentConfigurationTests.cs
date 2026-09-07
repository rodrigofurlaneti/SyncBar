using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class AsaasIntegrationPaymentConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(AsaasIntegrationPayment))!;

        [Fact]
        public void Configure_ShouldMapToAsaasIntegrationPaymentTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("asaasintegrationpayment");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(AsaasIntegrationPayment.Id));
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_BranchIdAndCustomerOrderId_ShouldBeRequired_CustomerId_ShouldBeOptional()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.CustomerOrderId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.CustomerId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_AsaasPaymentId_ShouldBeRequiredWithMaxLength50()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationPayment.AsaasPaymentId))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_BillingType_ShouldBeRequiredWithMaxLength30()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationPayment.BillingType))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_Status_ShouldBeRequiredWithMaxLength50()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationPayment.Status))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_ValueAndNetValue_ShouldHavePrecision18And2()
        {
            var value = EntityType.FindProperty(nameof(AsaasIntegrationPayment.Value))!;
            var netValue = EntityType.FindProperty(nameof(AsaasIntegrationPayment.NetValue))!;

            value.IsNullable.Should().BeFalse();
            value.GetPrecision().Should().Be(18);
            value.GetScale().Should().Be(2);

            netValue.IsNullable.Should().BeTrue();
            netValue.GetPrecision().Should().Be(18);
            netValue.GetScale().Should().Be(2);
        }

        [Fact]
        public void Configure_DueDate_ShouldBeRequiredDatetime6_PaymentDate_ShouldBeNullable()
        {
            var dueDate = EntityType.FindProperty(nameof(AsaasIntegrationPayment.DueDate))!;
            var paymentDate = EntityType.FindProperty(nameof(AsaasIntegrationPayment.PaymentDate))!;

            dueDate.IsNullable.Should().BeFalse();
            dueDate.GetColumnType().Should().Be("datetime(6)");

            paymentDate.IsNullable.Should().BeTrue();
            paymentDate.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_PixQrCodeBase64AndPixPayload_ShouldHaveTextColumnTypes()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.PixQrCodeBase64))!.GetColumnType().Should().Be("longtext");
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.PixPayload))!.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_InvoiceUrlAndBankSlipUrl_ShouldHaveMaxLength500()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.InvoiceUrl))!.GetMaxLength().Should().Be(500);
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.BankSlipUrl))!.GetMaxLength().Should().Be(500);
        }

        [Fact]
        public void Configure_InstallmentCount_ShouldBeRequiredWithDefaultValue1()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationPayment.InstallmentCount))!;

            property.IsNullable.Should().BeFalse();
            property.GetDefaultValue().Should().Be(1);
        }

        [Fact]
        public void Configure_CreditCardToken_ShouldHaveMaxLength150()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationPayment.CreditCardToken))!.GetMaxLength().Should().Be(150);
        }

        [Fact]
        public void Configure_CreatedAtUpdatedAtIsActive_ShouldMatchAuditPattern()
        {
            var createdAt = EntityType.FindProperty(nameof(AsaasIntegrationPayment.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(AsaasIntegrationPayment.UpdatedAt))!;
            var isActive = EntityType.FindProperty(nameof(AsaasIntegrationPayment.IsActive))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_AsaasIntegrationPayment_AsaasPaymentId" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(AsaasIntegrationPayment.AsaasPaymentId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationPayment_Branch" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationPayment.BranchId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationPayment_CustomerOrder" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationPayment.CustomerOrderId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationPayment_Customer" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationPayment.CustomerId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToBranchCustomerOrderAndCustomer()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationPayment_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationPayment.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationPayment_CustomerOrder" &&
                fk.PrincipalEntityType.ClrType == typeof(CustomerOrder) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationPayment.CustomerOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationPayment_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationPayment.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
