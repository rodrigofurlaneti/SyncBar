using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CustomerOrderConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CustomerOrder))!;

        [Fact]
        public void Configure_ShouldMapToCustomerOrderTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("customerorder");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CustomerOrder.Id));
            EntityType.FindProperty(nameof(CustomerOrder.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_OpenedAt_ShouldBeRequired_ClosedAt_ShouldBeNullable()
        {
            var openedAt = EntityType.FindProperty(nameof(CustomerOrder.OpenedAt))!;
            var closedAt = EntityType.FindProperty(nameof(CustomerOrder.ClosedAt))!;

            openedAt.IsNullable.Should().BeFalse();
            openedAt.GetColumnType().Should().Be("datetime(6)");

            closedAt.IsNullable.Should().BeTrue();
            closedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_MonetaryAmounts_ShouldHaveDecimal18_2ColumnType()
        {
            EntityType.FindProperty(nameof(CustomerOrder.SubtotalAmount))!.GetColumnType().Should().Be("decimal(18,2)");
            EntityType.FindProperty(nameof(CustomerOrder.SubtotalAmount))!.IsNullable.Should().BeFalse();

            EntityType.FindProperty(nameof(CustomerOrder.DiscountAmount))!.GetColumnType().Should().Be("decimal(18,2)");
            EntityType.FindProperty(nameof(CustomerOrder.DiscountAmount))!.IsNullable.Should().BeFalse();

            EntityType.FindProperty(nameof(CustomerOrder.ServiceFeeAmount))!.GetColumnType().Should().Be("decimal(18,2)");
            EntityType.FindProperty(nameof(CustomerOrder.ServiceFeeAmount))!.IsNullable.Should().BeFalse();

            EntityType.FindProperty(nameof(CustomerOrder.TotalAmount))!.GetColumnType().Should().Be("decimal(18,2)");
            EntityType.FindProperty(nameof(CustomerOrder.TotalAmount))!.IsNullable.Should().BeFalse();

            EntityType.FindProperty(nameof(CustomerOrder.CreditLimitAmount))!.GetColumnType().Should().Be("decimal(18,2)");
            EntityType.FindProperty(nameof(CustomerOrder.CreditLimitAmount))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_OrderTypeId_ShouldBeRequiredTinyint_OrderOriginId_ShouldBeRequiredBigint()
        {
            var orderTypeId = EntityType.FindProperty(nameof(CustomerOrder.OrderTypeId))!;
            var orderOriginId = EntityType.FindProperty(nameof(CustomerOrder.OrderOriginId))!;

            orderTypeId.IsNullable.Should().BeFalse();
            orderTypeId.GetColumnType().Should().Be("tinyint");

            orderOriginId.IsNullable.Should().BeFalse();
            orderOriginId.GetColumnType().Should().Be("bigint");
        }

        [Fact]
        public void Configure_OptionalCustomerAndDeliveryFields_ShouldHaveExpectedColumnTypes()
        {
            EntityType.FindProperty(nameof(CustomerOrder.CustomerName))!.GetColumnType().Should().Be("varchar(150)");
            EntityType.FindProperty(nameof(CustomerOrder.CustomerPhone))!.GetColumnType().Should().Be("varchar(20)");
            EntityType.FindProperty(nameof(CustomerOrder.DeliveryAddress))!.GetColumnType().Should().Be("varchar(300)");
            EntityType.FindProperty(nameof(CustomerOrder.Notes))!.GetColumnType().Should().Be("varchar(500)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(CustomerOrder.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(CustomerOrder.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_BranchId" && i.Properties.Single().Name == nameof(CustomerOrder.BranchId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_DiningTableId" && i.Properties.Single().Name == nameof(CustomerOrder.DiningTableId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_ComandaId" && i.Properties.Single().Name == nameof(CustomerOrder.ComandaId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_EmployeeId" && i.Properties.Single().Name == nameof(CustomerOrder.EmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_OrderStatusId" && i.Properties.Single().Name == nameof(CustomerOrder.OrderStatusId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_OpenedAt" && i.Properties.Single().Name == nameof(CustomerOrder.OpenedAt));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_CustomerId" && i.Properties.Single().Name == nameof(CustomerOrder.CustomerId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_CreatedAt" && i.Properties.Single().Name == nameof(CustomerOrder.CreatedAt));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CustomerOrder_OrderOriginId" && i.Properties.Single().Name == nameof(CustomerOrder.OrderOriginId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_CustomerOrder_CreatedAt_OrderStatusId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(CustomerOrder.CreatedAt),
                    nameof(CustomerOrder.OrderStatusId)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToRelatedEntities()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_DiningTable" &&
                fk.PrincipalEntityType.ClrType == typeof(DiningTable) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.DiningTableId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_Comanda" &&
                fk.PrincipalEntityType.ClrType == typeof(Comanda) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.ComandaId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_Employee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.EmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_OrderStatus" &&
                fk.PrincipalEntityType.ClrType == typeof(OrderStatus) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.OrderStatusId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerOrder_OrderOrigin" &&
                fk.PrincipalEntityType.ClrType == typeof(OrderOrigin) &&
                fk.Properties.Single().Name == nameof(CustomerOrder.OrderOriginId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configure_ItemsNavigation_ShouldCascadeDeleteAndUseFieldAccessMode()
        {
            var navigation = EntityType.FindNavigation(nameof(CustomerOrder.Items));

            navigation.Should().NotBeNull();
            navigation!.ForeignKey.GetConstraintName().Should().Be("FK_OrderItem_CustomerOrder");
            navigation.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
            navigation.ForeignKey.Properties.Single().Name.Should().Be(nameof(OrderItem.CustomerOrderId));
            navigation.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        }
    }
}
