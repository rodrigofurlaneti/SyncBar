using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodOrderConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodOrder))!;

        [Fact]
        public void Configure_ShouldMapToIfoodOrderTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodOrder");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodOrder.Id));
            EntityType.FindProperty(nameof(IfoodOrder.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_RequiredIdentifierFields_ShouldHaveExpectedMaxLengths()
        {
            var ifoodOrderId = EntityType.FindProperty(nameof(IfoodOrder.IfoodOrderId))!;
            var merchantId = EntityType.FindProperty(nameof(IfoodOrder.MerchantId))!;
            var ifoodOrderType = EntityType.FindProperty(nameof(IfoodOrder.IfoodOrderType))!;
            var status = EntityType.FindProperty(nameof(IfoodOrder.Status))!;

            ifoodOrderId.IsNullable.Should().BeFalse();
            ifoodOrderId.GetMaxLength().Should().Be(100);

            merchantId.IsNullable.Should().BeFalse();
            merchantId.GetMaxLength().Should().Be(100);

            ifoodOrderType.IsNullable.Should().BeFalse();
            ifoodOrderType.GetMaxLength().Should().Be(30);

            status.IsNullable.Should().BeFalse();
            status.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_OptionalIdentifierFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodOrder.DisplayId))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(IfoodOrder.DeliveredBy))!.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_OrderTiming_ShouldBeRequiredWithMaxLength20AndDefaultImmediate()
        {
            var property = EntityType.FindProperty(nameof(IfoodOrder.OrderTiming))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(20);
            property.GetDefaultValue().Should().Be("IMMEDIATE");
        }

        [Fact]
        public void Configure_DateFields_ShouldHaveExpectedNullability()
        {
            EntityType.FindProperty(nameof(IfoodOrder.PreparationStartDateTime))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(IfoodOrder.PreparationStartDateTime))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(IfoodOrder.ConfirmDeadlineAt))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodOrder.ConfirmDeadlineAt))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(IfoodOrder.ConfirmedAt))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(IfoodOrder.ConfirmedAt))!.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodOrder.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodOrder.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_IfoodOrder_IfoodOrderId" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(IfoodOrder.IfoodOrderId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodOrder_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodOrder.BranchId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodOrder_CustomerOrderId" &&
                i.Properties.Single().Name == nameof(IfoodOrder.CustomerOrderId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToBranchAndCustomerOrder()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodOrder_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodOrder.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodOrder_CustomerOrder" &&
                fk.PrincipalEntityType.ClrType == typeof(CustomerOrder) &&
                fk.Properties.Single().Name == nameof(IfoodOrder.CustomerOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
