using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class ShiftClosingStatusConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(ShiftClosingStatus))!;

        [Fact]
        public void Configure_ShouldMapToShiftClosingStatusTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("shiftclosingstatus");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(ShiftClosingStatus.Id));
            EntityType.FindProperty(nameof(ShiftClosingStatus.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_Name_ShouldBeRequiredVarchar50()
        {
            var name = EntityType.FindProperty(nameof(ShiftClosingStatus.Name))!;

            name.IsNullable.Should().BeFalse();
            name.GetColumnType().Should().Be("varchar(50)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(ShiftClosingStatus.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(ShiftClosingStatus.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(ShiftClosingStatus.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        // Lookup somente leitura: sem índices e sem FKs declarados na configuração real.
        [Fact]
        public void Configure_ShouldNotDeclareAnyForeignKey()
        {
            EntityType.GetForeignKeys().Should().BeEmpty();
        }
    }
}
