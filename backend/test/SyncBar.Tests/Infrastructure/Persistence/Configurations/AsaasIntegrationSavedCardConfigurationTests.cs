using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class AsaasIntegrationSavedCardConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(AsaasIntegrationSavedCard))!;

        [Fact]
        public void Configure_ShouldMapToAsaasIntegrationSavedCardTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("asaasintegrationsavedcard");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(AsaasIntegrationSavedCard.Id));
            EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CustomerId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.CustomerId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_CreditCardToken_ShouldBeRequiredWithMaxLength150()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.CreditCardToken))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(150);
        }

        [Fact]
        public void Configure_CardBrand_ShouldBeRequiredWithMaxLength50()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.CardBrand))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(50);
        }

        [Fact]
        public void Configure_Last4Digits_ShouldBeRequiredWithMaxLength10()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.Last4Digits))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(10);
        }

        [Fact]
        public void Configure_CreatedAtUpdatedAtIsActive_ShouldMatchAuditPattern()
        {
            var createdAt = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.UpdatedAt))!;
            var isActive = EntityType.FindProperty(nameof(AsaasIntegrationSavedCard.IsActive))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnCustomerId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationSavedCard_Customer" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationSavedCard.CustomerId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToCustomer()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationSavedCard_Customer" &&
                fk.PrincipalEntityType.ClrType == typeof(Customer) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationSavedCard.CustomerId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
