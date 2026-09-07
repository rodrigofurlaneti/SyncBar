using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodShippingDeliveryConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodShippingDelivery))!;

        [Fact]
        public void Configure_ShouldMapToIfoodShippingDeliveryTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodShippingDelivery");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodShippingDelivery.Id));
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CustomerAndAddressFields_ShouldBeRequiredWithExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerName))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerName))!.GetMaxLength().Should().Be(150);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerPhoneAreaCode))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerPhoneAreaCode))!.GetMaxLength().Should().Be(5);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerPhoneNumber))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.CustomerPhoneNumber))!.GetMaxLength().Should().Be(20);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.PostalCode))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.PostalCode))!.GetMaxLength().Should().Be(15);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.StreetName))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.StreetName))!.GetMaxLength().Should().Be(200);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.StreetNumber))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.StreetNumber))!.GetMaxLength().Should().Be(20);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.Neighborhood))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Neighborhood))!.GetMaxLength().Should().Be(100);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.City))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.City))!.GetMaxLength().Should().Be(100);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.State))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.State))!.GetMaxLength().Should().Be(2);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.Country))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Country))!.GetMaxLength().Should().Be(60);
        }

        [Fact]
        public void Configure_OptionalTextFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodShippingDelivery.OrderReference))!.GetMaxLength().Should().Be(150);
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Complement))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Reference))!.GetMaxLength().Should().Be(200);
            EntityType.FindProperty(nameof(IfoodShippingDelivery.TrackingUrl))!.GetMaxLength().Should().Be(500);
            EntityType.FindProperty(nameof(IfoodShippingDelivery.CancellationReason))!.GetMaxLength().Should().Be(300);
        }

        [Fact]
        public void Configure_MerchantFee_ShouldBeRequiredDecimal18_2()
        {
            var property = EntityType.FindProperty(nameof(IfoodShippingDelivery.MerchantFee))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("decimal(18,2)");
        }

        [Fact]
        public void Configure_QuoteIdIfoodDeliveryIdAndStatus_ShouldBeRequiredWithExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodShippingDelivery.QuoteId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.QuoteId))!.GetMaxLength().Should().Be(100);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.IfoodDeliveryId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.IfoodDeliveryId))!.GetMaxLength().Should().Be(100);

            EntityType.FindProperty(nameof(IfoodShippingDelivery.Status))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodShippingDelivery.Status))!.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_RequestedAt_ShouldBeRequired_CancelledAt_ShouldBeNullable()
        {
            var requestedAt = EntityType.FindProperty(nameof(IfoodShippingDelivery.RequestedAt))!;
            var cancelledAt = EntityType.FindProperty(nameof(IfoodShippingDelivery.CancelledAt))!;

            requestedAt.IsNullable.Should().BeFalse();
            requestedAt.GetColumnType().Should().Be("datetime(6)");

            cancelledAt.IsNullable.Should().BeTrue();
            cancelledAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodShippingDelivery.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodShippingDelivery.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnBranchIdAndUniqueIndexOnIfoodDeliveryId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodShippingDelivery_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodShippingDelivery.BranchId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_IfoodShippingDelivery_IfoodDeliveryId" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(IfoodShippingDelivery.IfoodDeliveryId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodShippingDelivery_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodShippingDelivery.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
