using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class CustomerAppUserConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(CustomerAppUser))!;

        [Fact]
        public void Configure_ShouldMapToCustomerAppUserTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("customerappuser");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(CustomerAppUser.Id));
            EntityType.FindProperty(nameof(CustomerAppUser.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CompanyId_ShouldBeRequired_BranchIdCustomerId_ShouldBeOptional()
        {
            EntityType.FindProperty(nameof(CustomerAppUser.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(CustomerAppUser.BranchId))!.IsNullable.Should().BeTrue();
            EntityType.FindProperty(nameof(CustomerAppUser.CustomerId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_UserName_ShouldBeRequiredVarchar100()
        {
            var property = EntityType.FindProperty(nameof(CustomerAppUser.UserName))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(100)");
        }

        [Fact]
        public void Configure_Email_ShouldBeRequiredVarchar150()
        {
            var property = EntityType.FindProperty(nameof(CustomerAppUser.Email))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(150)");
        }

        [Fact]
        public void Configure_PasswordHash_ShouldBeRequiredVarchar500()
        {
            var property = EntityType.FindProperty(nameof(CustomerAppUser.PasswordHash))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(500)");
        }

        [Fact]
        public void Configure_FailedAccessCount_ShouldBeRequiredWithDefaultValue0()
        {
            var property = EntityType.FindProperty(nameof(CustomerAppUser.FailedAccessCount))!;

            property.IsNullable.Should().BeFalse();
            property.GetDefaultValue().Should().Be(0);
        }

        [Fact]
        public void Configure_LockoutEndAtAndLastLoginAt_ShouldBeNullableDatetime6()
        {
            EntityType.FindProperty(nameof(CustomerAppUser.LockoutEndAt))!.GetColumnType().Should().Be("datetime(6)");
            EntityType.FindProperty(nameof(CustomerAppUser.LockoutEndAt))!.IsNullable.Should().BeTrue();

            EntityType.FindProperty(nameof(CustomerAppUser.LastLoginAt))!.GetColumnType().Should().Be("datetime(6)");
            EntityType.FindProperty(nameof(CustomerAppUser.LastLoginAt))!.IsNullable.Should().BeTrue();
        }

        // CreatedAt também usa HasDefaultValueSql("CURRENT_TIMESTAMP(6)") em produção, removido pelo
        // harness de testes — validamos apenas tipo de coluna e obrigatoriedade.
        [Fact]
        public void Configure_CreatedAt_ShouldBeRequiredDatetime6_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(CustomerAppUser.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(CustomerAppUser.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredWithDefaultTrue()
        {
            var isActive = EntityType.FindProperty(nameof(CustomerAppUser.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyBranchAndCustomer()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAppUser_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(CustomerAppUser.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAppUser_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(CustomerAppUser.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_CustomerAppUser_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(CustomerAppUser.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
