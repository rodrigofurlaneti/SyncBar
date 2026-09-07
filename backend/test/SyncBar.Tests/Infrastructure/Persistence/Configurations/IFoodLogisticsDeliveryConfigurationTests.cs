using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodLogisticsDeliveryConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodLogisticsDelivery))!;

        [Fact]
        public void Configure_ShouldMapToIfoodLogisticsDeliveryTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodLogisticsDelivery");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodLogisticsDelivery.Id));
            EntityType.FindProperty(nameof(IfoodLogisticsDelivery.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_DriverAndStatusFields_ShouldBeRequiredWithExpectedMaxLengths()
        {
            var driverName = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.DriverName))!;
            var driverPhone = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.DriverPhone))!;
            var driverVehicleType = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.DriverVehicleType))!;
            var status = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.Status))!;

            driverName.IsNullable.Should().BeFalse();
            driverName.GetMaxLength().Should().Be(150);

            driverPhone.IsNullable.Should().BeFalse();
            driverPhone.GetMaxLength().Should().Be(30);

            driverVehicleType.IsNullable.Should().BeFalse();
            driverVehicleType.GetMaxLength().Should().Be(30);

            status.IsNullable.Should().BeFalse();
            status.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_AssignedAt_ShouldBeRequired_OtherTimelineDates_ShouldBeNullable()
        {
            var assignedAt = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.AssignedAt))!;
            assignedAt.IsNullable.Should().BeFalse();
            assignedAt.GetColumnType().Should().Be("datetime(6)");

            foreach (var propertyName in new[]
            {
                nameof(IfoodLogisticsDelivery.GoingToOriginAt),
                nameof(IfoodLogisticsDelivery.ArrivedAtOriginAt),
                nameof(IfoodLogisticsDelivery.DispatchedAt),
                nameof(IfoodLogisticsDelivery.ArrivedAtDestinationAt),
                nameof(IfoodLogisticsDelivery.DeliveryCodeVerifiedAt)
            })
            {
                var property = EntityType.FindProperty(propertyName)!;
                property.IsNullable.Should().BeTrue();
                property.GetColumnType().Should().Be("datetime(6)");
            }
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodLogisticsDelivery.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnIfoodOrderIdAndIndexOnBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_IfoodLogisticsDelivery_IfoodOrderId" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(IfoodLogisticsDelivery.IfoodOrderId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodLogisticsDelivery_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodLogisticsDelivery.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToIfoodOrderAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodLogisticsDelivery_IfoodOrder" &&
                fk.PrincipalEntityType.ClrType == typeof(IfoodOrder) &&
                fk.Properties.Single().Name == nameof(IfoodLogisticsDelivery.IfoodOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodLogisticsDelivery_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodLogisticsDelivery.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
