using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodPizzaMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodPizzaMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodPizzaMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodPizzaMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodPizzaMapping.Id));
            EntityType.FindProperty(nameof(IfoodPizzaMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodPizzaId_ShouldBeRequiredVarchar100()
        {
            var property = EntityType.FindProperty(nameof(IfoodPizzaMapping.IfoodPizzaId))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(100)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodPizzaMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodPizzaMapping.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodPizzaMapping_PizzaConfigurationId_BranchId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodPizzaMapping.PizzaConfigurationId),
                    nameof(IfoodPizzaMapping.BranchId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodPizzaMapping_BranchId" &&
                i.Properties.Single().Name == nameof(IfoodPizzaMapping.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToPizzaConfigurationAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodPizzaMapping_PizzaConfiguration" &&
                fk.PrincipalEntityType.ClrType == typeof(PizzaConfiguration) &&
                fk.Properties.Single().Name == nameof(IfoodPizzaMapping.PizzaConfigurationId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_IfoodPizzaMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodPizzaMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configure_ElementsNavigation_ShouldCascadeDeleteAndUseFieldAccessMode()
        {
            var navigation = EntityType.FindNavigation(nameof(IfoodPizzaMapping.Elements));

            navigation.Should().NotBeNull();
            navigation!.ForeignKey.GetConstraintName().Should().Be("FK_IfoodPizzaElementMapping_IfoodPizzaMapping");
            navigation.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
            navigation.ForeignKey.Properties.Single().Name.Should().Be(nameof(IfoodPizzaElementMapping.IfoodPizzaMappingId));
            navigation.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        }
    }
}
