using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class DiningTableConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(DiningTable))!;

        [Fact]
        public void Configure_ShouldMapToDiningTableTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("DiningTable");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(DiningTable.Id));
            EntityType.FindProperty(nameof(DiningTable.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_BooleanFlags_ShouldBeRequiredWithExpectedDefaults()
        {
            var isQrViewEnabled = EntityType.FindProperty(nameof(DiningTable.IsQrViewEnabled))!;
            var isCameraInputEnabled = EntityType.FindProperty(nameof(DiningTable.IsCameraInputEnabled))!;
            var isBarcodeEnabled = EntityType.FindProperty(nameof(DiningTable.IsBarcodeEnabled))!;
            var isQrCodeEnabled = EntityType.FindProperty(nameof(DiningTable.IsQrCodeEnabled))!;

            isQrViewEnabled.IsNullable.Should().BeFalse();
            isQrViewEnabled.GetDefaultValue().Should().Be(true);

            isCameraInputEnabled.IsNullable.Should().BeFalse();
            isCameraInputEnabled.GetDefaultValue().Should().Be(false);

            isBarcodeEnabled.IsNullable.Should().BeFalse();
            isBarcodeEnabled.GetDefaultValue().Should().Be(false);

            isQrCodeEnabled.IsNullable.Should().BeFalse();
            isQrCodeEnabled.GetDefaultValue().Should().Be(false);
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(DiningTable.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(DiningTable.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_DiningTable_TableStatusId" &&
                i.Properties.Single().Name == nameof(DiningTable.TableStatusId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_DiningTable_QrToken" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(DiningTable.QrToken));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_DiningTable_BranchId_Number" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(DiningTable.BranchId),
                    nameof(DiningTable.Number)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToBranchAndTableStatus()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_DiningTable_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(DiningTable.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_DiningTable_TableStatus" &&
                fk.PrincipalEntityType.ClrType == typeof(TableStatus) &&
                fk.Properties.Single().Name == nameof(DiningTable.TableStatusId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
