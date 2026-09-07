using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class TableItemTransferConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(TableItemTransfer))!;

        [Fact]
        public void Configure_ShouldMapToTableItemTransferTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("tableitemtransfer");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(TableItemTransfer.Id));
            // A configuração real não chama ValueGeneratedOnAdd() explicitamente para o Id, mas a
            // convenção do EF Core já trata chaves primárias `long`/`int` como geradas pelo banco.
            EntityType.FindProperty(nameof(TableItemTransfer.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_AllForeignKeyIdentifiers_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(TableItemTransfer.CustomerOrderId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(TableItemTransfer.CustomerOrderItemId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(TableItemTransfer.SourceDiningTableId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(TableItemTransfer.TargetDiningTableId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(TableItemTransfer.EmployeeId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAtAndIsActive_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(TableItemTransfer.CreatedAt))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(TableItemTransfer.IsActive))!.IsNullable.Should().BeFalse();
        }

        // Nenhuma das chamadas HasOne() nesta configuração define HasConstraintName — os nomes das
        // constraints ficam a cargo da convenção do EF Core, então validamos apenas o principal, a(s)
        // propriedade(s) de FK e o comportamento de delete, sem fixar um nome de constraint específico.
        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToCustomerOrder()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.PrincipalEntityType.ClrType == typeof(CustomerOrder) &&
                fk.Properties.Single().Name == nameof(TableItemTransfer.CustomerOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToSourceAndTargetDiningTable()
        {
            var foreignKeys = EntityType.GetForeignKeys()
                .Where(fk => fk.PrincipalEntityType.ClrType == typeof(DiningTable))
                .ToList();

            foreignKeys.Should().Contain(fk =>
                fk.Properties.Single().Name == nameof(TableItemTransfer.SourceDiningTableId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.Properties.Single().Name == nameof(TableItemTransfer.TargetDiningTableId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToEmployee()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(TableItemTransfer.EmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
