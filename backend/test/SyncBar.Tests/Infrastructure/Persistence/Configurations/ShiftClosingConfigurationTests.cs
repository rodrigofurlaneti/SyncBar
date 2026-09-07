using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class ShiftClosingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(ShiftClosing))!;

        [Fact]
        public void Configure_ShouldMapToShiftClosingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("shiftclosing");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(ShiftClosing.Id));
            EntityType.FindProperty(nameof(ShiftClosing.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_MonetaryTotals_ShouldBeRequiredDecimal18_2()
        {
            foreach (var propertyName in new[]
            {
                nameof(ShiftClosing.TotalOpeningAmount),
                nameof(ShiftClosing.TotalExpectedAmount),
                nameof(ShiftClosing.TotalRealizedAmount),
                nameof(ShiftClosing.TotalDifferenceAmount)
            })
            {
                var property = EntityType.FindProperty(propertyName)!;
                property.IsNullable.Should().BeFalse();
                property.GetColumnType().Should().Be("decimal(18,2)");
            }
        }

        [Fact]
        public void Configure_Notes_ShouldBeOptionalVarchar500()
        {
            var notes = EntityType.FindProperty(nameof(ShiftClosing.Notes))!;

            notes.IsNullable.Should().BeTrue();
            notes.GetColumnType().Should().Be("varchar(500)");
        }

        [Fact]
        public void Configure_PeriodStart_ShouldBeRequired_PeriodEnd_ShouldBeNullable()
        {
            var periodStart = EntityType.FindProperty(nameof(ShiftClosing.PeriodStart))!;
            var periodEnd = EntityType.FindProperty(nameof(ShiftClosing.PeriodEnd))!;

            periodStart.IsNullable.Should().BeFalse();
            periodStart.GetColumnType().Should().Be("datetime(6)");

            periodEnd.IsNullable.Should().BeTrue();
            periodEnd.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(ShiftClosing.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(ShiftClosing.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(ShiftClosing.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_ShiftClosing_BranchId" && i.Properties.Single().Name == nameof(ShiftClosing.BranchId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_ShiftClosing_ShiftClosingStatusId" && i.Properties.Single().Name == nameof(ShiftClosing.ShiftClosingStatusId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_ShiftClosing_OpenedByEmployeeId" && i.Properties.Single().Name == nameof(ShiftClosing.OpenedByEmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_ShiftClosing_ClosedByEmployeeId" && i.Properties.Single().Name == nameof(ShiftClosing.ClosedByEmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_ShiftClosing_PeriodStart" && i.Properties.Single().Name == nameof(ShiftClosing.PeriodStart));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToBranchStatusAndEmployees()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosing_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(ShiftClosing.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosing_ShiftClosingStatus" &&
                fk.PrincipalEntityType.ClrType == typeof(ShiftClosingStatus) &&
                fk.Properties.Single().Name == nameof(ShiftClosing.ShiftClosingStatusId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosing_OpenedByEmployee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(ShiftClosing.OpenedByEmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosing_ClosedByEmployee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(ShiftClosing.ClosedByEmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
