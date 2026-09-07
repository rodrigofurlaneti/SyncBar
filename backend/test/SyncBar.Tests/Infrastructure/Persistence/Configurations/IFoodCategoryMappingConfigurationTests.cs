using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodCategoryMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodCategoryMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodCategoryMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodCategoryMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodCategoryMapping.Id));
            EntityType.FindProperty(nameof(IfoodCategoryMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodCategoryId_ShouldBeRequiredWithMaxLength100()
        {
            var property = EntityType.FindProperty(nameof(IfoodCategoryMapping.IfoodCategoryId))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodCategoryMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodCategoryMapping.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnCategoryIdAndBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodCategoryMapping_CategoryId_BranchId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodCategoryMapping.CategoryId),
                    nameof(IfoodCategoryMapping.BranchId)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCategoryAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodCategoryMapping_Category" &&
                fk.PrincipalEntityType.ClrType == typeof(Category) &&
                fk.Properties.Single().Name == nameof(IfoodCategoryMapping.CategoryId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodCategoryMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodCategoryMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
