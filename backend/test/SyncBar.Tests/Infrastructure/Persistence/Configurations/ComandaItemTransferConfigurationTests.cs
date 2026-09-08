using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class ComandaItemTransferConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(ComandaItemTransfer))!;

        [Fact]
        public void Configure_ShouldMapToComandaItemTransferTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("comandaitemtransfer");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(ComandaItemTransfer.Id));
            EntityType.FindProperty(nameof(ComandaItemTransfer.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_AllForeignKeyIdentifiers_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(ComandaItemTransfer.CustomerOrderId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(ComandaItemTransfer.CustomerOrderItemId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(ComandaItemTransfer.SourceComandaId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(ComandaItemTransfer.TargetComandaId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(ComandaItemTransfer.EmployeeId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreatedAtAndIsActive_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(ComandaItemTransfer.CreatedAt))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(ComandaItemTransfer.IsActive))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_IsActive_ShouldDefaultToTrue()
        {
            EntityType.FindProperty(nameof(ComandaItemTransfer.IsActive))!.GetDefaultValue().Should().Be(true);
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
                fk.Properties.Single().Name == nameof(ComandaItemTransfer.CustomerOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }

        // Achado de revisão: diferente da configuração irmã (TableItemTransferConfiguration, que
        // define FK Restrict para SourceDiningTableId/TargetDiningTableId), esta configuração NÃO
        // declara HasOne(...) para SourceComandaId/TargetComandaId — só Property(...).IsRequired().
        // Documentado aqui como o comportamento real (sem integridade referencial nessas duas
        // colunas), não necessariamente o desejado — reportado ao usuário para decisão.
        [Fact]
        public void Configure_SourceAndTargetComandaId_ShouldHaveNoForeignKeyConstraint()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().NotContain(fk => fk.Properties.Any(p => p.Name == nameof(ComandaItemTransfer.SourceComandaId)));
            foreignKeys.Should().NotContain(fk => fk.Properties.Any(p => p.Name == nameof(ComandaItemTransfer.TargetComandaId)));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToEmployee()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.PrincipalEntityType.ClrType == typeof(Employee) &&
                fk.Properties.Single().Name == nameof(ComandaItemTransfer.EmployeeId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
