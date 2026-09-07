using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CustomerAddressConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CustomerAddress))!;

        [Fact]
        public void Configure_ShouldMapToCustomerAddressTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("customeraddress");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CustomerAddress.Id));
            EntityType.FindProperty(nameof(CustomerAddress.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CompanyId_ShouldBeRequired_BranchIdCustomerIdLastOrderId_ShouldBeOptional()
        {
            EntityType.FindProperty(nameof(CustomerAddress.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(CustomerAddress.BranchId))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(CustomerAddress.CustomerId))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(CustomerAddress.LastOrderId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_Street_ShouldBeRequiredWithMaxLength500()
        {
            var street = EntityType.FindProperty(nameof(CustomerAddress.Street))!;

            street.IsNullable.Should().BeFalse();
            street.GetMaxLength().Should().Be(500);
        }

        [Fact]
        public void Configure_Number_ShouldBeRequiredWithMaxLength50()
        {
            var number = EntityType.FindProperty(nameof(CustomerAddress.Number))!;

            number.IsNullable.Should().BeFalse();
            number.GetMaxLength().Should().Be(50);
        }

        // A configuração real chama builder.Property(x => x.Supplement).HasMaxLength(...) DUAS vezes
        // (500, depois 9) — a segunda chamada sobrescreve a primeira, então o max length efetivo no
        // modelo é 9. Isso é provavelmente um bug de copy/paste na configuração de produção, mas o
        // teste precisa refletir o que o EF realmente construiu, não a intenção.
        [Fact]
        public void Configure_Supplement_ShouldBeRequiredWithMaxLength9DueToSecondHasMaxLengthCallOverridingTheFirst()
        {
            var supplement = EntityType.FindProperty(nameof(CustomerAddress.Supplement))!;

            supplement.IsNullable.Should().BeFalse();
            supplement.GetMaxLength().Should().Be(9);
        }

        [Fact]
        public void Configure_LastOrderAt_ShouldBeNullable()
        {
            EntityType.FindProperty(nameof(CustomerAddress.LastOrderAt))!.IsNullable.Should().BeTrue();
        }

        // A produção usa HasDefaultValueSql("CURRENT_TIMESTAMP(6)") para CreatedAt — o harness de
        // testes (SqliteCompatibleModelCustomizer) remove esse default para todas as entidades, então
        // não é possível verificar esse valor aqui; verificamos apenas a obrigatoriedade da coluna.
        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(CustomerAddress.CreatedAt))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_UpdatedAt_ShouldBeNullable()
        {
            EntityType.FindProperty(nameof(CustomerAddress.UpdatedAt))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredWithDefaultTrue()
        {
            var isActive = EntityType.FindProperty(nameof(CustomerAddress.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyBranchCustomerAndOrder()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAddress_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(CustomerAddress.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAddress_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(CustomerAddress.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAddress_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(CustomerAddress.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAddress_Order" &&
                fk.PrincipalEntityType.ClrType == typeof(CustomerOrder) &&
                fk.Properties.Single().Name == nameof(CustomerAddress.LastOrderId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
