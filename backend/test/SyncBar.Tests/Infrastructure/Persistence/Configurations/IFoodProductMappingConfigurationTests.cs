using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodProductMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodProductMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodProductMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodProductMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodProductMapping.Id));
            EntityType.FindProperty(nameof(IfoodProductMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodItemIdAndIfoodProductId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(IfoodProductMapping.IfoodItemId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodProductMapping.IfoodProductId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodProductMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodProductMapping.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodProductMapping_ProductId_BranchId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodProductMapping.ProductId),
                    nameof(IfoodProductMapping.BranchId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodProductMapping_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodProductMapping.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToProductAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodProductMapping_Product" &&
                fk.PrincipalEntityType.ClrType == typeof(Product) &&
                fk.Properties.Single().Name == nameof(IfoodProductMapping.ProductId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodProductMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodProductMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
