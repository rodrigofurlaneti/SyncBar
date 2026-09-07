using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CashMovementConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CashMovement))!;

        [Fact]
        public void Configure_ShouldMapToCashMovementTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("cashmovement");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CashMovement.Id));
            EntityType.FindProperty(nameof(CashMovement.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_Amount_ShouldBeRequiredDecimal18_2()
        {
            var amount = EntityType.FindProperty(nameof(CashMovement.Amount))!;

            amount.IsNullable.Should().BeFalse();
            amount.GetColumnType().Should().Be("decimal(18,2)");
        }

        [Fact]
        public void Configure_Description_ShouldBeOptionalVarchar300()
        {
            var description = EntityType.FindProperty(nameof(CashMovement.Description))!;

            description.IsNullable.Should().BeTrue();
            description.GetColumnType().Should().Be("varchar(300)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequiredDatetime6_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(CashMovement.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(CashMovement.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineExpectedIndexes()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashMovement_CashSessionId" && i.Properties.Single().Name == nameof(CashMovement.CashSessionId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashMovement_CashMovementTypeId" && i.Properties.Single().Name == nameof(CashMovement.CashMovementTypeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashMovement_SaleId" && i.Properties.Single().Name == nameof(CashMovement.SaleId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashMovement_EmployeeId" && i.Properties.Single().Name == nameof(CashMovement.EmployeeId));
            indexes.Should().Contain(i => i.GetDatabaseName() == "IX_CashMovement_CreatedAt" && i.Properties.Single().Name == nameof(CashMovement.CreatedAt));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCashSessionCashMovementTypeSaleAndEmployee()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashMovement_CashSession" &&
                fk.PrincipalEntityType.ClrType == typeof(CashSession) &&
                fk.Properties.Single().Name == nameof(CashMovement.CashSessionId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashMovement_CashMovementType" &&
                fk.PrincipalEntityType.ClrType == typeof(CashMovementType) &&
                fk.Properties.Single().Name == nameof(CashMovement.CashMovementTypeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashMovement_Sale" &&
                fk.PrincipalEntityType.ClrType == typeof(Sale) &&
                fk.Properties.Single().Name == nameof(CashMovement.SaleId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashMovement_Employee" &&
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(CashMovement.EmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
