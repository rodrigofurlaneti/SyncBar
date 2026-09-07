using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class IFoodIntegrationSettingConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(IfoodIntegrationSetting))!;

        [Fact]
        public void Configure_ShouldMapToIfoodIntegrationSettingTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("IfoodIntegrationSetting");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(IfoodIntegrationSetting.Id));
            EntityType.FindProperty(nameof(IfoodIntegrationSetting.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_ClientIdAndIfoodCustomerId_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(IfoodIntegrationSetting.ClientId))!.GetMaxLength().Should().Be(200);
            EntityType.FindProperty(nameof(IfoodIntegrationSetting.IfoodCustomerId))!.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_ClientSecretEncrypted_ShouldBeOptionalVarchar1000()
        {
            var property = EntityType.FindProperty(nameof(IfoodIntegrationSetting.ClientSecretEncrypted))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("varchar(1000)");
        }

        [Fact]
        public void Configure_Enabled_ShouldBeRequiredBit_LastConnectionTestSucceeded_ShouldBeNullableBit()
        {
            var enabled = EntityType.FindProperty(nameof(IfoodIntegrationSetting.Enabled))!;
            var lastConnectionTestSucceeded = EntityType.FindProperty(nameof(IfoodIntegrationSetting.LastConnectionTestSucceeded))!;

            enabled.IsNullable.Should().BeFalse();
            enabled.GetColumnType().Should().Be("bit");

            lastConnectionTestSucceeded.IsNullable.Should().BeTrue();
            lastConnectionTestSucceeded.GetColumnType().Should().Be("bit");
        }

        [Fact]
        public void Configure_LastConnectionTestAt_ShouldBeNullableDatetime6()
        {
            var property = EntityType.FindProperty(nameof(IfoodIntegrationSetting.LastConnectionTestAt))!;

            property.IsNullable.Should().BeTrue();
            property.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequired_UpdatedAt_ShouldBeNullable()
        {
            var createdAt = EntityType.FindProperty(nameof(IfoodIntegrationSetting.CreatedAt))!;
            var updatedAt = EntityType.FindProperty(nameof(IfoodIntegrationSetting.UpdatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnCompanyId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_IfoodIntegrationSetting_CompanyId" &&
                i.Properties.Single().Name == nameof(IfoodIntegrationSetting.CompanyId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeyToCompany()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().ContainSingle(fk =>
                fk.GetConstraintName() == "FK_IfoodIntegrationSetting_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(IfoodIntegrationSetting.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
