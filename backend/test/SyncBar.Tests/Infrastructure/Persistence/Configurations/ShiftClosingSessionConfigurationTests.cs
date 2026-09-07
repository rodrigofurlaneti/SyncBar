using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class ShiftClosingSessionConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(ShiftClosingSession))!;

        [Fact]
        public void Configure_ShouldMapToShiftClosingSessionTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("shiftclosingsession");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(ShiftClosingSession.Id));
            EntityType.FindProperty(nameof(ShiftClosingSession.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(ShiftClosingSession.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(ShiftClosingSession.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(ShiftClosingSession.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnShiftClosingIdAndCashSessionIdAndIndexOnCashSessionId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_ShiftClosingSession_ShiftClosingId_CashSessionId" &&
                i.IsUnique &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(ShiftClosingSession.ShiftClosingId),
                    nameof(ShiftClosingSession.CashSessionId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_ShiftClosingSession_CashSessionId" &&
                i.Properties.Single().Name == nameof(ShiftClosingSession.CashSessionId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToShiftClosingAndCashSession()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosingSession_ShiftClosing" &&
                fk.PrincipalEntityType.ClrType == typeof(ShiftClosing) &&
                fk.Properties.Single().Name == nameof(ShiftClosingSession.ShiftClosingId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_ShiftClosingSession_CashSession" &&
                fk.PrincipalEntityType.ClrType == typeof(CashSession) &&
                fk.Properties.Single().Name == nameof(ShiftClosingSession.CashSessionId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
