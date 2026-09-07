using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class AsaasIntegrationSettingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(AsaasIntegrationSetting))!;

        [Fact]
        public void Configure_ShouldMapToAsaasIntegrationSettingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("asaasintegrationsetting");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(AsaasIntegrationSetting.Id));
            EntityType.FindProperty(nameof(AsaasIntegrationSetting.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CompanyId_ShouldBeRequired_BranchId_ShouldBeOptional()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationSetting.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(AsaasIntegrationSetting.BranchId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_Environment_ShouldBeRequiredWithMaxLength20AndDefaultSandbox()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSetting.Environment))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(20);
            property.GetDefaultValue().Should().Be("Sandbox");
        }

        [Fact]
        public void Configure_ApiKeyEncrypted_ShouldBeRequiredVarchar1000()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSetting.ApiKeyEncrypted))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("varchar(1000)");
        }

        [Fact]
        public void Configure_WebhookSecretEncrypted_ShouldBeOptionalVarchar500()
        {
            var property = EntityType.FindProperty(nameof(AsaasIntegrationSetting.WebhookSecretEncrypted))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("varchar(500)");
        }

        [Fact]
        public void Configure_WalletId_ShouldHaveMaxLength100()
        {
            EntityType.FindProperty(nameof(AsaasIntegrationSetting.WalletId))!.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_CreatedAtUpdatedAtIsActive_ShouldMatchAuditPattern()
        {
            var createdAt = EntityType.FindProperty(nameof(AsaasIntegrationSetting.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(AsaasIntegrationSetting.UpdatedAt))!;
            var isActive = EntityType.FindProperty(nameof(AsaasIntegrationSetting.IsActive))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnCompanyIdAndBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationSetting_Company" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationSetting.CompanyId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_AsaasIntegrationSetting_Branch" &&
                i.Properties.Single().Name == nameof(AsaasIntegrationSetting.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationSetting_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationSetting.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_AsaasIntegrationSetting_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(AsaasIntegrationSetting.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
