using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class AsaasIntegrationCustomerConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(AsaasIntegrationCustomer))!;

        [Fact]
        public void Configure_ShouldMapToAsaasIntegrationCustomerTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("asaasintegrationcustomer");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(AsaasIntegrationCustomer.Id));
            EntityType.FindProperty(nameof(AsaasIntegrationCustomer.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CustomerIdAndCompanyId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationCustomer.CustomerId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(AsaasIntegrationCustomer.CompanyId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_AsaasCustomerId_ShouldBeRequiredWithMaxLength50()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationCustomer.AsaasCustomerId))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequiredDatetime6()
        {
            var createdAt = EntityType.FindProperty(nameof(AsaasIntegrationCustomer.CreatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_UpdatedAt_ShouldBeNullableDatetime6()
        {
            var updatedAt = EntityType.FindProperty(nameof(AsaasIntegrationCustomer.UpdatedAt))!;

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyintWithDefaultTrue()
        {
            var isActive = EntityType.FindProperty(nameof(AsaasIntegrationCustomer.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineUniqueIndexOnCustomerIdAndCompanyId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "UQ_AsaasIntegrationCustomer_Customer_Company" &&
                i.IsUnique &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(AsaasIntegrationCustomer.CustomerId),
                    nameof(AsaasIntegrationCustomer.CompanyId)
                }));
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnAsaasCustomerIdAndCompanyId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationCustomer_AsaasCustomerId" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationCustomer.AsaasCustomerId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationCustomer_Company" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationCustomer.CompanyId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCustomerAndCompany()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationCustomer_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationCustomer.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationCustomer_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationCustomer.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
