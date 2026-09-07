using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationMerchantMappingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationMerchantMapping))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationMerchantMappingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationmerchantmapping");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationMerchantMapping.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_CompanyIdBranchIdAndKeetaMerchantId_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.KeetaMerchantId))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_InternalMerchantIdAndStoreName_ShouldBeRequiredWithExpectedMaxLengths()
        {
            var internalMerchantId = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.InternalMerchantId))!;
            var storeName = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.StoreName))!;

            internalMerchantId.IsNullable.Should().BeFalse();
            internalMerchantId.GetMaxLength().Should().Be(100);

            storeName.IsNullable.Should().BeFalse();
            storeName.GetMaxLength().Should().Be(255);
        }

        [Fact]
        public void Configure_OptionalStringFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.TimeZone))!.GetMaxLength().Should().Be(50);
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.MenuBaseUrl))!.GetMaxLength().Should().Be(255);
            EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.WebhookUrl))!.GetMaxLength().Should().Be(255);
        }

        [Fact]
        public void Configure_IsAuthorizedAndIsOnboarded_ShouldBeRequiredTinyintWithExpectedDefaults()
        {
            var isAuthorized = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.IsAuthorized))!;
            var isOnboarded = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.IsOnboarded))!;

            isAuthorized.IsNullable.Should().BeFalse();
            isAuthorized.GetColumnType().Should().Be("tinyint(1)");
            isAuthorized.GetDefaultValue().Should().Be(true);

            isOnboarded.IsNullable.Should().BeFalse();
            isOnboarded.GetColumnType().Should().Be("tinyint(1)");
            isOnboarded.GetDefaultValue().Should().Be(false);
        }

        [Fact]
        public void Configure_LastMenuSyncAtUtc_ShouldBeNullableDatetime6()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.LastMenuSyncAtUtc))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAtUtc_ShouldBeRequiredDatetime6()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationMerchantMapping.CreatedAtUtc))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineUniqueCompositeIndexAndInternalMerchantIdIndex()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "uid_keeta_merchant" &&
                i.IsUnique &&
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(KeetaIntegrationMerchantMapping.CompanyId),
                    nameof(KeetaIntegrationMerchantMapping.BranchId),
                    nameof(KeetaIntegrationMerchantMapping.KeetaMerchantId)
                }));

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "idx_internal_merchant" &&
                i.Properties.Single().Name == nameof(KeetaIntegrationMerchantMapping.InternalMerchantId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaMapping_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationMerchantMapping.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaMapping_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationMerchantMapping.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
