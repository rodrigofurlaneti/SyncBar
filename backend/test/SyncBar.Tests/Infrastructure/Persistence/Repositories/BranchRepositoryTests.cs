using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class BranchRepositoryTests : RepositoryTestBase
    {
        private readonly BranchRepository _repository;

        public BranchRepositoryTests()
        {
            _repository = new BranchRepository(Context);
        }

        private static Branch CreateBranch(long companyId = 1, string name = "Matriz") =>
            Branch.Create(companyId, name, null, null, null, null, null, null, null, null).Value;

        private async Task<Branch> SeedAsync(Branch branch)
        {
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            Context.Entry(branch).State = EntityState.Detached;
            return branch;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedBranch()
        {
            var branch = await SeedAsync(CreateBranch());

            var result = await _repository.GetByIdAsync(branch.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_DifferentTenant_ReturnsNull()
        {
            var branch = await SeedAsync(CreateBranch(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(branch.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NoTenantSet_ReturnsBranchRegardlessOfCompany()
        {
            var branch = await SeedAsync(CreateBranch(companyId: 5));
            TenantService.CompanyId = null;

            var result = await _repository.GetByIdAsync(branch.Id);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedBranch()
        {
            var branch = await SeedAsync(CreateBranch());

            var result = await _repository.GetByIdForUpdateAsync(branch.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_MultipleActiveBranches_ReturnsOrderedByName()
        {
            var zebra = await SeedAsync(CreateBranch(companyId: 5, name: "Zebra"));
            var alpha = await SeedAsync(CreateBranch(companyId: 5, name: "Alpha"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(alpha.Id);
            result.ElementAt(1).Id.Should().Be(zebra.Id);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveBranch_IsExcluded()
        {
            var branch = await SeedAsync(CreateBranch(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(branch.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoBranchesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_DifferentTenant_ReturnsEmptyList()
        {
            await SeedAsync(CreateBranch(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidBranch_PersistsToDatabase()
        {
            var branch = CreateBranch(companyId: 7, name: "Filial Nova");

            await _repository.AddAsync(branch);
            await Context.SaveChangesAsync();

            var persisted = await Context.Branchs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Filial Nova");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
