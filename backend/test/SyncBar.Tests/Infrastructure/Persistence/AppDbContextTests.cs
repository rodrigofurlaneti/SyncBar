using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence
{
    public sealed class AppDbContextTests : Repositories.RepositoryTestBase
    {
        [Fact]
        public async Task CommitAsync_PendingAddedEntity_PersistsAndReturnsAffectedRowCount()
        {
            var feature = AppFeature.Create("FEATURE_COMMIT", "Feature Commit").Value;
            await Context.AddAsync(feature);

            var affectedRows = await Context.CommitAsync();

            affectedRows.Should().Be(1);
            var persisted = await Context.AppFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "FEATURE_COMMIT");
            persisted.Should().NotBeNull();
        }

        [Fact]
        public async Task CommitAsync_NoPendingChanges_ReturnsZero()
        {
            var affectedRows = await Context.CommitAsync();

            affectedRows.Should().Be(0);
        }

        [Fact]
        public async Task CompanyScopedQueryFilter_TenantSet_ReturnsOnlyMatchingCompanyRows()
        {
            var categoryTenantOne = Category.Create(1, "Category Tenant 1", 1).Value;
            var categoryTenantTwo = Category.Create(2, "Category Tenant 2", 1).Value;
            await Context.AddRangeAsync(categoryTenantOne, categoryTenantTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = 1;

            var result = await Context.Categories.ToListAsync();

            result.Should().ContainSingle();
            result[0].CompanyId.Should().Be(1);
        }

        [Fact]
        public async Task CompanyScopedQueryFilter_TenantNotSet_ReturnsAllCompaniesRows()
        {
            var categoryTenantOne = Category.Create(1, "Category Tenant 1", 1).Value;
            var categoryTenantTwo = Category.Create(2, "Category Tenant 2", 1).Value;
            await Context.AddRangeAsync(categoryTenantOne, categoryTenantTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = null;

            var result = await Context.Categories.ToListAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task BranchScopedQueryFilter_TenantSet_ReturnsOnlyRowsOfBranchesBelongingToCompany()
        {
            var companyOne = Company.Create("Legal One", "Trade One", "11111111000101", null, null).Value;
            var companyTwo = Company.Create("Legal Two", "Trade Two", "22222222000102", null, null).Value;
            await Context.AddRangeAsync(companyOne, companyTwo);
            await Context.SaveChangesAsync();

            var branchOne = Branch.Create(companyOne.Id, "Branch One", null, null, null, null, null, null, null, null).Value;
            var branchTwo = Branch.Create(companyTwo.Id, "Branch Two", null, null, null, null, null, null, null, null).Value;
            await Context.AddRangeAsync(branchOne, branchTwo);
            await Context.SaveChangesAsync();

            var comandaOne = Comanda.Create(branchOne.Id, 1, "CMD-1").Value;
            var comandaTwo = Comanda.Create(branchTwo.Id, 1, "CMD-2").Value;
            await Context.AddRangeAsync(comandaOne, comandaTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = companyOne.Id;

            var result = await Context.Comandas.ToListAsync();

            result.Should().ContainSingle();
            result[0].BranchId.Should().Be(branchOne.Id);
        }

        [Fact]
        public async Task BranchScopedQueryFilter_TenantNotSet_ReturnsAllBranchesRows()
        {
            var companyOne = Company.Create("Legal One", "Trade One", "11111111000101", null, null).Value;
            var companyTwo = Company.Create("Legal Two", "Trade Two", "22222222000102", null, null).Value;
            await Context.AddRangeAsync(companyOne, companyTwo);
            await Context.SaveChangesAsync();

            var branchOne = Branch.Create(companyOne.Id, "Branch One", null, null, null, null, null, null, null, null).Value;
            var branchTwo = Branch.Create(companyTwo.Id, "Branch Two", null, null, null, null, null, null, null, null).Value;
            await Context.AddRangeAsync(branchOne, branchTwo);
            await Context.SaveChangesAsync();

            var comandaOne = Comanda.Create(branchOne.Id, 1, "CMD-1").Value;
            var comandaTwo = Comanda.Create(branchTwo.Id, 1, "CMD-2").Value;
            await Context.AddRangeAsync(comandaOne, comandaTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = null;

            var result = await Context.Comandas.ToListAsync();

            result.Should().HaveCount(2);
        }
    }
}
