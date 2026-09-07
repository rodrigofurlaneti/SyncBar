using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class DiningAreaConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(DiningArea))!;

        [Fact]
        public void Configure_ShouldMapToDiningAreaTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("diningarea");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(DiningArea.Id));
            EntityType.FindProperty(nameof(DiningArea.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_BranchId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(DiningArea.BranchId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_Name_ShouldBeRequiredVarchar100()
        {
            var name = EntityType.FindProperty(nameof(DiningArea.Name))!;

            name.IsNullable.Should().BeFalse();
            name.GetColumnType().Should().Be("varchar(100)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(DiningArea.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(DiningArea.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(DiningArea.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_DiningArea_BranchId" && i.Properties.Single().Name == nameof(DiningArea.BranchId));
        }

        // A configuração real não declara nenhum HasOne/HasForeignKey para BranchId.
        [Fact]
        public void Configure_ShouldNotDeclareAnyForeignKey()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
