using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodSettlementConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodSettlement))!;

        [Fact]
        public void Configure_ShouldMapToIfoodSettlementTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodSettlement");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodSettlement.Id));
            EntityType.FindProperty(nameof(IfoodSettlement.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_IfoodSettlementIdTypeAndStatus_ShouldBeRequiredWithExpectedMaxLengths()
        {
            var ifoodSettlementId = EntityType.FindProperty(nameof(IfoodSettlement.IfoodSettlementId))!;
            var type = EntityType.FindProperty(nameof(IfoodSettlement.Type))!;
            var status = EntityType.FindProperty(nameof(IfoodSettlement.Status))!;

            ifoodSettlementId.IsNullable.Should().BeFalse();
            ifoodSettlementId.GetMaxLength().Should().Be(100);

            type.IsNullable.Should().BeFalse();
            type.GetMaxLength().Should().Be(30);

            status.IsNullable.Should().BeFalse();
            status.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_OptionalFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodSettlement.Product))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(IfoodSettlement.BankCode))!.GetMaxLength().Should().Be(20);
            EntityType.FindProperty(nameof(IfoodSettlement.BankAgency))!.GetMaxLength().Should().Be(20);
            EntityType.FindProperty(nameof(IfoodSettlement.BankAccount))!.GetMaxLength().Should().Be(30);
        }

        [Fact]
        public void Configure_Amount_ShouldBeRequiredDecimal18_2()
        {
            var amount = EntityType.FindProperty(nameof(IfoodSettlement.Amount))!;

            amount.IsNullable.Should().BeFalse();
            amount.GetColumnType().Should().Be("decimal(18,2)");
        }

        [Fact]
        public void Configure_PaymentDate_ShouldBeNullableDatetime6()
        {
            var property = EntityType.FindProperty(nameof(IfoodSettlement.PaymentDate))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_RawPayload_ShouldBeRequiredText()
        {
            var rawPayload = EntityType.FindProperty(nameof(IfoodSettlement.RawPayload))!;

            rawPayload.IsNullable.Should().BeFalse();
            rawPayload.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodSettlement.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodSettlement.UpdatedAt))!;

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
                i.GetDatabaseName() == "IX_IfoodSettlement_BranchId_IfoodSettlementId" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodSettlement.BranchId),
                    nameof(IfoodSettlement.IfoodSettlementId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodSettlement_BranchId_PaymentDate" &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(IfoodSettlement.BranchId),
                    nameof(IfoodSettlement.PaymentDate)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodSettlement_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(IfoodSettlement.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
