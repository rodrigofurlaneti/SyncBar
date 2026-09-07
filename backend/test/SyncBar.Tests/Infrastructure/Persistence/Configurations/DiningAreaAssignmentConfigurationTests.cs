using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class DiningAreaAssignmentConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(DiningAreaAssignment))!;

        [Fact]
        public void Configure_ShouldMapToDiningAreaAssignmentTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("diningareaassignment");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(DiningAreaAssignment.Id));
            EntityType.FindProperty(nameof(DiningAreaAssignment.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_DiningAreaIdAndEmployeeId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(DiningAreaAssignment.DiningAreaId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(DiningAreaAssignment.EmployeeId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_StartAt_ShouldBeRequired_EndAt_ShouldBeNullable()
        {
            var startAt = EntityType.FindProperty(nameof(DiningAreaAssignment.StartAt))!;
            var endAt = EntityType.FindProperty(nameof(DiningAreaAssignment.EndAt))!;

            startAt.IsNullable.Should().BeFalse();
            startAt.GetColumnType().Should().Be("datetime(6)");

            endAt.IsNullable.Should().BeTrue();
            endAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(DiningAreaAssignment.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(DiningAreaAssignment.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(DiningAreaAssignment.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnDiningAreaIdAndEmployeeId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_DiningAreaAssignment_DiningAreaId" && i.Properties.Single().Name == nameof(DiningAreaAssignment.DiningAreaId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_DiningAreaAssignment_EmployeeId" && i.Properties.Single().Name == nameof(DiningAreaAssignment.EmployeeId));
        }

        // A configuração real não declara nenhum HasOne/HasForeignKey para esta entidade —
        // DiningAreaId e EmployeeId ficam sem constraint declarada explicitamente no modelo.
        [Fact]
        public void Configure_ShouldNotDeclareAnyForeignKey()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
