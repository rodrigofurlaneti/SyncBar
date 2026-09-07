using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodPizzaElementMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodPizzaElementMapping))!;

        [Fact]
        public void Configure_ShouldMapToIfoodPizzaElementMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodPizzaElementMapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodPizzaElementMapping.Id));
            EntityType.FindProperty(nameof(IfoodPizzaElementMapping.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_KindAndLocalId_ShouldBeRequired()
        {
            var kind = EntityType.FindProperty(nameof(IfoodPizzaElementMapping.Kind))!;
            var localId = EntityType.FindProperty(nameof(IfoodPizzaElementMapping.LocalId))!;

            kind.IsNullable.Should().BeFalse();
            kind.GetColumnType().Should().Be("tinyint");

            localId.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_IfoodElementId_ShouldBeRequiredVarchar100()
        {
            var property = EntityType.FindProperty(nameof(IfoodPizzaElementMapping.IfoodElementId))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(100)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodPizzaElementMapping.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodPizzaElementMapping.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnMappingIdKindAndLocalId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodPizzaElementMapping_MappingId_Kind_LocalId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodPizzaElementMapping.IfoodPizzaMappingId),
                    nameof(IfoodPizzaElementMapping.Kind),
                    nameof(IfoodPizzaElementMapping.LocalId)
                }));
        }

        // A própria IfoodPizzaElementMappingConfiguration não declara HasOne/HasForeignKey — a FK
        // pra IfoodPizzaMapping é criada do lado dono da coleção, em IfoodPizzaMappingConfiguration
        // (HasMany(x => x.Elements).WithOne()...). Ainda assim ela aparece no modelo real construído
        // para esta entidade, então validamos aqui a partir do IEntityType de IfoodPizzaElementMapping.
        [Fact]
        public void Configure_ShouldHaveCascadingForeignKeyToIfoodPizzaMappingDeclaredBySiblingConfiguration()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodPizzaElementMapping_IfoodPizzaMapping" &&
                fk.PrincipalEntityType.ClrType == typeof(IfoodPizzaMapping) &&
                fk.Properties.Single().Name == nameof(IfoodPizzaElementMapping.IfoodPizzaMappingId) &&
                fk.DeleteBehavior == DeleteBehavior.Cascade);
        }
    }
}
