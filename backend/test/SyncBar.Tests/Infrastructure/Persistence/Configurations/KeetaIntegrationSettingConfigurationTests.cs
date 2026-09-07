using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationSettingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationSetting))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationSettingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationsetting");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationSetting.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationSetting.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_CompanyIdAndBranchId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationSetting.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationSetting.BranchId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_OptionalCredentialFields_ShouldHaveMaxLength255()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationSetting.ClientId))!.GetMaxLength().Should().Be(255);
            EntityType.FindProperty(nameof(KeetaIntegrationSetting.ClientSecret))!.GetMaxLength().Should().Be(255);
            EntityType.FindProperty(nameof(KeetaIntegrationSetting.AppId))!.GetMaxLength().Should().Be(255);
        }

        // BaseUrl é `string` (não anulável) sem chamada explícita a IsRequired() na configuração —
        // mas o projeto de Infrastructure tem <Nullable>enable</Nullable>, então o EF Core infere a
        // obrigatoriedade automaticamente a partir da anotação de nullable reference type do
        // property, sem precisar de configuração explícita.
        [Fact]
        public void Configure_BaseUrl_ShouldBeRequiredWithMaxLength255AndDefaultKeetaUrl()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationSetting.BaseUrl))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(255);
            property.GetDefaultValue().Should().Be("https://open.mykeeta.com/api/open/opendelivery");
        }

        [Fact]
        public void Configure_CurrentAccessToken_ShouldBeOptionalText()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationSetting.CurrentAccessToken))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("text");
        }

        [Fact]
        public void Configure_TokenExpiresAtUtc_ShouldBeNullable_UpdatedAtUtc_ShouldBeRequired()
        {
            var tokenExpiresAtUtc = EntityType.FindProperty(nameof(KeetaIntegrationSetting.TokenExpiresAtUtc))!;
            var updatedAtUtc = EntityType.FindProperty(nameof(KeetaIntegrationSetting.UpdatedAtUtc))!;

            tokenExpiresAtUtc.IsNullable.Should().BeTrue();
            tokenExpiresAtUtc.GetColumnType().Should().Be("datetime(6)");

            updatedAtUtc.IsNullable.Should().BeFalse();
            updatedAtUtc.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnCompanyIdAndBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_KeetaSetting_Company" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationSetting.CompanyId));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_KeetaSetting_Branch" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationSetting.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaSetting_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationSetting.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaSetting_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationSetting.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
