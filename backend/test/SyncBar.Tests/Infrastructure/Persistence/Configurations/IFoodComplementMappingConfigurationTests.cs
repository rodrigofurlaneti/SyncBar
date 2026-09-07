using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodComplementMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodComplementMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodComplementMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodComplementMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodComplementMapping.Id));
            EntityType.FindProperty(nameof(IfoodComplementMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodOptionIdAndIfoodProductId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(IfoodComplementMapping.IfoodOptionId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodComplementMapping.IfoodProductId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodComplementMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodComplementMapping.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodComplementMapping_ComplementId_BranchId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodComplementMapping.ComplementId),
                    nameof(IfoodComplementMapping.BranchId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodComplementMapping_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodComplementMapping.BranchId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodComplementMapping_IfoodOptionId" &&
                i.Properties.Single().Name == nameof(IfoodComplementMapping.IfoodOptionId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToComplementAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodComplementMapping_Complement" &&
                fk.PrincipalEntityType.ClrType == typeof(Complement) &&
                fk.Properties.Single().Name == nameof(IfoodComplementMapping.ComplementId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodComplementMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodComplementMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
