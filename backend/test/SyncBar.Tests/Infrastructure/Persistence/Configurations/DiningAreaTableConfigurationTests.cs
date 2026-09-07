using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class DiningAreaTableConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(DiningAreaTable))!;

        [Fact]
        public void Configure_ShouldMapToDiningAreaTableTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("diningareatable");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(DiningAreaTable.Id));
            EntityType.FindProperty(nameof(DiningAreaTable.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_DiningAreaIdAndDiningTableId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(DiningAreaTable.DiningAreaId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(DiningAreaTable.DiningTableId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(DiningAreaTable.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(DiningAreaTable.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(DiningAreaTable.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnDiningTableIdAndIndexOnDiningAreaId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UK_DiningAreaTable_Table" &&
                i.IsUnique &&
                i.Properties.Single().Name == nameof(DiningAreaTable.DiningTableId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_DiningAreaTable_DiningAreaId" &&
                i.Properties.Single().Name == nameof(DiningAreaTable.DiningAreaId));
        }

        // A configuração real não declara nenhum HasOne/HasForeignKey para DiningAreaId/DiningTableId.
        [Fact]
        public void Configure_ShouldNotDeclareAnyForeignKey()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
