using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodMerchantMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodMerchantMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodMerchantMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodMerchantMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodMerchantMapping.Id));
            EntityType.FindProperty(nameof(IfoodMerchantMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_MerchantIdAndMerchantUuid_ShouldHaveMaxLength100()
        {
            EntityType.FindProperty(nameof(IfoodMerchantMapping.MerchantId))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(IfoodMerchantMapping.MerchantUuid))!.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_PreparationTimeMinutes_ShouldBeNullable()
        {
            EntityType.FindProperty(nameof(IfoodMerchantMapping.PreparationTimeMinutes))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodMerchantMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodMerchantMapping.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodMerchantMapping_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodMerchantMapping.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodMerchantMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodMerchantMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
