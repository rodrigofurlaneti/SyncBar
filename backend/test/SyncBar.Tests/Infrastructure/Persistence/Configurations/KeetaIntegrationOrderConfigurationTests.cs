using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationOrderConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationOrder))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationOrderTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationorder");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationOrder.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationOrder.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_RelationalIdentifiers_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.CustomerId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.CustomerOrderId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.KeetaMerchantId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_StringFields_ShouldBeRequiredWithExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.KeetaOrderId))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.DisplayId))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.InternalMerchantId))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.Status))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.OrderType))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.DeliveredBy))!.GetMaxLength().Should().Be(50);

            foreach (var propertyName in new[]
            {
                nameof(KeetaIntegrationOrder.KeetaOrderId),
                nameof(KeetaIntegrationOrder.DisplayId),
                nameof(KeetaIntegrationOrder.InternalMerchantId),
                nameof(KeetaIntegrationOrder.Status),
                nameof(KeetaIntegrationOrder.OrderType),
                nameof(KeetaIntegrationOrder.DeliveredBy)
            })
            {
                EntityType.FindProperty(propertyName)!.IsNullable.Should().BeFalse();
            }
        }

        [Fact]
        public void Configure_OrderAmount_ShouldBeRequiredDecimal10_2()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationOrder.OrderAmount))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("decimal(10,2)");
        }

        [Fact]
        public void Configure_Currency_ShouldBeRequiredWithMaxLength3AndDefaultBrl()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationOrder.Currency))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(3);
            property.GetDefaultValue().Should().Be("BRL");
        }

        [Fact]
        public void Configure_RawOrderJson_ShouldBeRequiredJson()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationOrder.RawOrderJson))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("json");
        }

        [Fact]
        public void Configure_DateFields_ShouldHaveExpectedNullability()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.OrderCreatedAtUtc))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.OrderCreatedAtUtc))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(KeetaIntegrationOrder.CreatedAtUtc))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationOrder.CreatedAtUtc))!.GetColumnType().Should().Be("datetime(6)");

            foreach (var propertyName in new[]
            {
                nameof(KeetaIntegrationOrder.ConfirmedAtUtc),
                nameof(KeetaIntegrationOrder.ReadyForPickupAtUtc),
                nameof(KeetaIntegrationOrder.ConcludedAtUtc)
            })
            {
                var property = EntityType.FindProperty(propertyName)!;
                property.IsNullable.Should().BeTrue();
                property.GetColumnType().Should().Be("datetime(6)");
            }
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnKeetaOrderIdAndIndexOnKeetaMerchantId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "uid_keeta_order_id" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(KeetaIntegrationOrder.KeetaOrderId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "idx_order_merchant" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationOrder.KeetaMerchantId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyBranchCustomerAndCustomerOrder()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaOrder_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrder.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaOrder_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrder.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaOrder_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrder.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaOrder_CustomerOrder" &&
                fk.PrincipalEntityType.ClrType == typeof(CustomerOrder) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationOrder.CustomerOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
