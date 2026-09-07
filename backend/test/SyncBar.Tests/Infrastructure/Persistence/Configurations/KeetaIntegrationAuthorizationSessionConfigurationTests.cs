using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    public sealed class KeetaIntegrationAuthorizationSessionConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(KeetaIntegrationAuthorizationSession))!;

        [Fact]
        public void Configure_ShouldMapToKeetaIntegrationAuthorizationSessionTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("keetaintegrationauthorizationsession");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(KeetaIntegrationAuthorizationSession.Id));

            var idProperty = EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.Id))!;
            idProperty.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
            // Nota: Id é `long` (herdado de AggregateRoot), mas a configuração real chama
            // HasMaxLength(36) nele mesmo assim — provavelmente um resquício de quando o Id ainda
            // era pensado como GUID/string. A anotação fica gravada no modelo (mesmo sem efeito
            // prático numa coluna numérica), então é o que o EF realmente construiu.
            idProperty.GetMaxLength().Should().Be(36);
        }

        [Fact]
        public void Configure_CompanyIdBranchIdAndOperationType_ShouldBeRequired()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.CompanyId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.BranchId))!.IsNullable.Should().BeFalse();
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.OperationType))!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void Configure_AuthId_ShouldBeRequiredWithMaxLength100()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.AuthId))!;

            property.IsNullable.Should().BeFalse();
            property.GetMaxLength().Should().Be(100);
        }

        [Fact]
        public void Configure_OptionalStringFields_ShouldHaveExpectedMaxLengths()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.State))!.GetMaxLength().Should().Be(255);
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.AuthorizationCode))!.GetMaxLength().Should().Be(255);
        }

        [Fact]
        public void Configure_KeetaMerchantId_ShouldBeNullable()
        {
            EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.KeetaMerchantId))!.IsNullable.Should().BeTrue();
        }

        [Fact]
        public void Configure_IsProcessed_ShouldBeRequiredTinyintWithDefaultFalse()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.IsProcessed))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("tinyint(1)");
            property.GetDefaultValue().Should().Be(false);
        }

        [Fact]
        public void Configure_CreatedAtUtc_ShouldBeRequiredDatetime6()
        {
            var property = EntityType.FindProperty(nameof(KeetaIntegrationAuthorizationSession.CreatedAtUtc))!;

            property.IsNullable.Should().BeFalse();
            property.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_ShouldDefineIndexOnAuthId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "idx_auth_id" &&
                !i.IsUnique &&
                i.Properties.Single().Name == nameof(KeetaIntegrationAuthorizationSession.AuthId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaSession_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationAuthorizationSession.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_KeetaSession_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(KeetaIntegrationAuthorizationSession.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
