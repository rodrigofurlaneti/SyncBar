using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodFinancialEventConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodFinancialEvent))!;

        [Fact]
        public void Configure_ShouldMapToIfoodFinancialEventTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodFinancialEvent");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodFinancialEvent.Id));
            EntityType.FindProperty(nameof(IfoodFinancialEvent.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodEventIdAndName_ShouldBeRequiredWithExpectedMaxLengths()
        {
            var ifoodEventId = EntityType.FindProperty(nameof(IfoodFinancialEvent.IfoodEventId))!;
            var name = EntityType.FindProperty(nameof(IfoodFinancialEvent.Name))!;

            ifoodEventId.IsNullable.Should().BeFalse();
            ifoodEventId.GetMaxLength().Should().Be(100);

            name.IsNullable.Should().BeFalse();
            name.GetMaxLength().Should().Be(150);
        }

        [Fact]
        public void Configure_OptionalTextFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodFinancialEvent.Description))!.GetMaxLength().Should().Be(500);
            EntityType.FindProperty(nameof(IfoodFinancialEvent.Trigger))!.GetMaxLength().Should().Be(100);
            EntityType.FindProperty(nameof(IfoodFinancialEvent.ReferenceType))!.GetMaxLength().Should().Be(30);
            EntityType.FindProperty(nameof(IfoodFinancialEvent.ReferenceId))!.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_Amount_ShouldBeRequiredDecimal18_2()
        {
            var amount = EntityType.FindProperty(nameof(IfoodFinancialEvent.Amount))!;

            amount.IsNullable.Should().BeFalse();
            amount.GetColumnType().Should().Be("decimal(18,2)");
        }

        [Fact]
        public void Configure_DateFields_ShouldHaveExpectedNullability()
        {
            EntityType.FindProperty(nameof(IfoodFinancialEvent.CompetenceDate))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodFinancialEvent.CompetenceDate))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(IfoodFinancialEvent.PeriodStart))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodFinancialEvent.PeriodStart))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(IfoodFinancialEvent.PeriodEnd))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(IfoodFinancialEvent.PeriodEnd))!.GetColumnType().Should().Be("datetime(6)");

            EntityType.FindProperty(nameof(IfoodFinancialEvent.SettlementExpectedDate))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(IfoodFinancialEvent.SettlementExpectedDate))!.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_RawPayload_ShouldBeRequiredText()
        {
            var rawPayload = EntityType.FindProperty(nameof(IfoodFinancialEvent.RawPayload))!;

            rawPayload.IsNullable.Should().BeFalse();
            rawPayload.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodFinancialEvent.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodFinancialEvent.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodFinancialEvent_BranchId_IfoodEventId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodFinancialEvent.BranchId),
                    nameof(IfoodFinancialEvent.IfoodEventId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodFinancialEvent_BranchId_CompetenceDate" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodFinancialEvent.BranchId),
                    nameof(IfoodFinancialEvent.CompetenceDate)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodFinancialEvent_ReferenceType_ReferenceId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodFinancialEvent.ReferenceType),
                    nameof(IfoodFinancialEvent.ReferenceId)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodFinancialEvent_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodFinancialEvent.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
