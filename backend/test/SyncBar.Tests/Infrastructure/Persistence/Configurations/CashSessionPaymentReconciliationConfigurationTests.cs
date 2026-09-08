using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CashSessionPaymentReconciliationConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CashSessionPaymentReconciliation))!;

        [Fact]
        public void Configure_ShouldMapToCashSessionPaymentReconciliationTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("cashsessionpaymentreconciliation");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CashSessionPaymentReconciliation.Id));
            EntityType.FindProperty(nameof(CashSessionPaymentReconciliation.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_MonetaryProperties_ShouldBeRequiredDecimal18_2()
        {
            foreach (var propertyName in new[]
            {
                nameof(CashSessionPaymentReconciliation.ExpectedAmount),
                nameof(CashSessionPaymentReconciliation.CountedAmount),
                nameof(CashSessionPaymentReconciliation.DifferenceAmount),
            })
            {
                var property = EntityType.FindProperty(propertyName)!;
                property.IsNullable.Should().BeFalse();
                property.GetColumnType().Should().Be("decimal(18,2)");
            }
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(CashSessionPaymentReconciliation.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(CashSessionPaymentReconciliation.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyint()
        {
            var isActive = EntityType.FindProperty(nameof(CashSessionPaymentReconciliation.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnCashSessionIdAndPaymentMethodIdAndIndexOnPaymentMethodId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_CashSessionPaymentReconciliation_CashSessionId_PaymentMethodId" &&
                i.IsUnique &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(CashSessionPaymentReconciliation.CashSessionId),
                    nameof(CashSessionPaymentReconciliation.PaymentMethodId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_CashSessionPaymentReconciliation_PaymentMethodId" &&
                i.Properties.Single().Name == nameof(CashSessionPaymentReconciliation.PaymentMethodId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCashSessionAndPaymentMethod()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSessionPaymentReconciliation_CashSession" &&
                fk.PrincipalEntityType.ClrType == typeof(CashSession) &&
                fk.Properties.Single().Name == nameof(CashSessionPaymentReconciliation.CashSessionId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CashSessionPaymentReconciliation_PaymentMethod" &&
                fk.PrincipalEntityType.ClrType == typeof(PaymentMethod) &&
                fk.Properties.Single().Name == nameof(CashSessionPaymentReconciliation.PaymentMethodId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
