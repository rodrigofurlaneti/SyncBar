using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodComplementGroupMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodComplementGroupMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodComplementGroupMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodComplementGroupMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodComplementGroupMapping.Id));
            EntityType.FindProperty(nameof(IfoodComplementGroupMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodOptionGroupId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(IfoodComplementGroupMapping.IfoodOptionGroupId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodComplementGroupMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodComplementGroupMapping.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodComplementGroupMapping_ComplementGroupId_BranchId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodComplementGroupMapping.ComplementGroupId),
                    nameof(IfoodComplementGroupMapping.BranchId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodComplementGroupMapping_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodComplementGroupMapping.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToComplementGroupAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodComplementGroupMapping_ComplementGroup" &&
                fk.PrincipalEntityType.ClrType == typeof(ComplementGroup) &&
                fk.Properties.Single().Name == nameof(IfoodComplementGroupMapping.ComplementGroupId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodComplementGroupMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodComplementGroupMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
