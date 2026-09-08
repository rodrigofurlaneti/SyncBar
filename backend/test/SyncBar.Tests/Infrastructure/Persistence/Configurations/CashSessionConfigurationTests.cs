using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CashSessionConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CashSession))!;

        [Fact]
        public void Configure_ShouldMapToCashSessionTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("cashsession");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CashSession.Id));
            EntityType.FindProperty(nameof(CashSession.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_OpeningAmount_ShouldBeRequired_OtherAmounts_ShouldBeNullableDecimal18_2()
        {
            var opening = EntityType.FindProperty(nameof(CashSession.OpeningAmount))!;
            var closing = EntityType.FindProperty(nameof(CashSession.ClosingAmount))!;
            var expected = EntityType.FindProperty(nameof(CashSession.ExpectedAmount))!;
            var difference = EntityType.FindProperty(nameof(CashSession.DifferenceAmount))!;
            var totalDifference = EntityType.FindProperty(nameof(CashSession.TotalDifferenceAmount))!;

            opening.IsNullable.Should().BeFalse();
            opening.GetColumnType().Should().Be("decimal(18,2)");

            closing.IsNullable.Should().BeTrue();
            closing.GetColumnType().Should().Be("decimal(18,2)");

            expected.IsNullable.Should().BeTrue();
            expected.GetColumnType().Should().Be("decimal(18,2)");

            difference.IsNullable.Should().BeTrue();
            difference.GetColumnType().Should().Be("decimal(18,2)");

            totalDifference.IsNullable.Should().BeTrue();
            totalDifference.GetColumnType().Should().Be("decimal(18,2)");
        }

        [Fact]
        public void Configure_OpenedAt_ShouldBeRequired_ClosedAt_ShouldBeNullable()
        {
            var openedAt = EntityType.FindProperty(nameof(CashSession.OpenedAt))!;
            var closedAt = EntityType.FindProperty(nameof(CashSession.ClosedAt))!;

            openedAt.IsNullable.Should().BeFalse();
            openedAt.GetColumnType().Should().Be("datetime(6)");

            closedAt.IsNullable.Should().BeTrue();
            closedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(CashSession.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(CashSession.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(CashSession.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_CashRegisterId" && i.Properties.Single().Name == nameof(CashSession.CashRegisterId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_CashSessionStatusId" && i.Properties.Single().Name == nameof(CashSession.CashSessionStatusId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_OpenedByEmployeeId" && i.Properties.Single().Name == nameof(CashSession.OpenedByEmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_ClosedByEmployeeId" && i.Properties.Single().Name == nameof(CashSession.ClosedByEmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_OpenedAt" && i.Properties.Single().Name == nameof(CashSession.OpenedAt));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashSession_CreatedAt" && i.Properties.Single().Name == nameof(CashSession.CreatedAt));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCashRegisterStatusAndEmployees()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSession_CashRegister" &&
                fk.PrincipalEntityType.ClrType == typeof(CashRegister) &&
                fk.Properties.Single().Name == nameof(CashSession.CashRegisterId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSession_CashSessionStatus" &&
                fk.PrincipalEntityType.ClrType == typeof(CashSessionStatus) &&
                fk.Properties.Single().Name == nameof(CashSession.CashSessionStatusId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSession_OpenedByEmployee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(CashSession.OpenedByEmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSession_ClosedByEmployee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(CashSession.ClosedByEmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
