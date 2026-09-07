using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SyncBar.Domain.Entities;
using SyncBar.Tests.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Configurations
{
    // OrderOriginConfiguration não é testável isoladamente (é só um método que recebe um
    // EntityTypeBuilder) — a forma real de exercitar cada linha dela é deixar o AppDbContext
    // construir o modelo de verdade (via RepositoryTestBase, que já aplica todas as
    // configurações da assembly) e inspecionar o resultado através de IEntityType.
    public sealed class OrderOriginConfigurationTests : RepositoryTestBase
    {
        private IEntityType EntityType => Context.Model.FindEntityType(typeof(OrderOrigin))!;

        [Fact]
        public void Configure_ShouldMapToOrderOriginTableWithIdAsPrimaryKey()
        {
            EntityType.GetTableName().Should().Be("orderorigin");
            var primaryKey = EntityType.FindPrimaryKey();
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(OrderOrigin.Id));
            EntityType.FindProperty(nameof(OrderOrigin.Id))!.ValueGenerated.Should().Be(ValueGenerated.OnAdd);
        }

        [Fact]
        public void Configure_CompanyIdAndBranchId_ShouldBeNullableBigint()
        {
            var companyId = EntityType.FindProperty(nameof(OrderOrigin.CompanyId))!;
            var branchId = EntityType.FindProperty(nameof(OrderOrigin.BranchId))!;

            companyId.IsNullable.Should().BeTrue();
            companyId.GetColumnType().Should().Be("bigint");
            branchId.IsNullable.Should().BeTrue();
            branchId.GetColumnType().Should().Be("bigint");
        }

        [Fact]
        public void Configure_Name_ShouldBeRequiredVarchar100()
        {
            var name = EntityType.FindProperty(nameof(OrderOrigin.Name))!;

            name.IsNullable.Should().BeFalse();
            name.GetColumnType().Should().Be("varchar(100)");
        }

        [Fact]
        public void Configure_CreatedAt_ShouldBeRequiredDatetime6()
        {
            var createdAt = EntityType.FindProperty(nameof(OrderOrigin.CreatedAt))!;

            createdAt.IsNullable.Should().BeFalse();
            createdAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_UpdatedAt_ShouldBeNullableDatetime6()
        {
            var updatedAt = EntityType.FindProperty(nameof(OrderOrigin.UpdatedAt))!;

            updatedAt.IsNullable.Should().BeTrue();
            updatedAt.GetColumnType().Should().Be("datetime(6)");
        }

        [Fact]
        public void Configure_IsActive_ShouldBeRequiredTinyintWithDefaultTrue()
        {
            var isActive = EntityType.FindProperty(nameof(OrderOrigin.IsActive))!;

            isActive.IsNullable.Should().BeFalse();
            isActive.GetColumnType().Should().Be("tinyint(1)");
            isActive.GetDefaultValue().Should().Be(true);
        }

        [Fact]
        public void Configure_ShouldDefineIndexesOnCompanyIdAndBranchId()
        {
            var indexes = EntityType.GetIndexes().ToList();

            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_OrderOrigin_CompanyId" &&
                i.Properties.Single().Name == nameof(OrderOrigin.CompanyId));
            indexes.Should().Contain(i =>
                i.GetDatabaseName() == "IX_OrderOrigin_BranchId" &&
                i.Properties.Single().Name == nameof(OrderOrigin.BranchId));
        }

        [Fact]
        public void Configure_ShouldDefineRestrictedForeignKeysToCompanyAndBranch()
        {
            var foreignKeys = EntityType.GetForeignKeys().ToList();

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_OrderOrigin_Company" &&
                fk.PrincipalEntityType.ClrType == typeof(Company) &&
                fk.Properties.Single().Name == nameof(OrderOrigin.CompanyId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);

            foreignKeys.Should().Contain(fk =>
                fk.GetConstraintName() == "FK_OrderOrigin_Branch" &&
                fk.PrincipalEntityType.ClrType == typeof(Branch) &&
                fk.Properties.Single().Name == nameof(OrderOrigin.BranchId) &&
                fk.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }
}
