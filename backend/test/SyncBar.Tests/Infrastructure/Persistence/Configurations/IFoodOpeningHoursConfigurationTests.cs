using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodOpeningHoursConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodOpeningHours))!;

        [Fact]
        public void Configure_ShouldMapToIfoodOpeningHoursTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodOpeningHours");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodOpeningHours.Id));
            EntityType.FindProperty(nameof(IfoodOpeningHours.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_DayOfWeekAndDurationMinutes_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(IfoodOpeningHours.DayOfWeek))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodOpeningHours.DurationMinutes))!.IsNullable.Should().BeFalse();
        }

        // Start é TimeSpan em produção (coluna time(0)); o SqliteCompatibleModelCustomizer do
        // harness de testes converte TimeSpan/TimeSpan? pra ticks (long) porque o provider SQLite
        // não ordena TimeSpan nativamente — por isso validamos obrigatoriedade e a presença do
        // conversor, e não o ClrType exato.
        [Fact]
        public void Configure_Start_ShouldBeRequiredAndConvertedForSqlite()
        {
            var start = EntityType.FindProperty(nameof(IfoodOpeningHours.Start))!;

            start.IsNullable.Should().BeFalse();
            start.GetValueConverter().Should().NotBeNull();
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodOpeningHours.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodOpeningHours.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnBranchIdAndDayOfWeek()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodOpeningHours_BranchId_DayOfWeek" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodOpeningHours.BranchId),
                    nameof(IfoodOpeningHours.DayOfWeek)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodOpeningHours_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodOpeningHours.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
