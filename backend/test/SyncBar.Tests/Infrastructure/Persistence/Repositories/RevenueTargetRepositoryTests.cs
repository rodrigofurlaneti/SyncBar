using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class RevenueTargetRepositoryTests : RepositoryTestBase
    {
        private readonly RevenueTargetRepository _repository;

        public RevenueTargetRepositoryTests()
        {
            _repository = new RevenueTargetRepository(Context);
        }

        private static RevenueTarget CreateTarget(long branchId = 1, int year = 2026, int month = 1) =>
            RevenueTarget.Create(branchId, year, month, 10000m).Value;

        private async Task<RevenueTarget> SeedAsync(RevenueTarget target)
        {
            await Context.AddAsync(target);
            await Context.SaveChangesAsync();
            Context.Entry(target).State = EntityState.Detached;
            return target;
        }

        [Fact]
        public async Task GetByBranchAndMonthAsync_ExistingActiveTarget_ReturnsUntrackedTarget()
        {
            var target = await SeedAsync(CreateTarget(branchId: 5, year: 2026, month: 3));

            var result = await _repository.GetByBranchAndMonthAsync(5, 2026, 3);

            result.Should().NotBeNull();
            result!.Id.Should().Be(target.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByBranchAndMonthAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateTarget(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByBranchAndMonthAsync(branch.Id, 2026, 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAndMonthAsync_DifferentMonth_ReturnsNull()
        {
            await SeedAsync(CreateTarget(branchId: 5, year: 2026, month: 3));

            var result = await _repository.GetByBranchAndMonthAsync(5, 2026, 4);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAndMonthForUpdateAsync_ExistingActiveTarget_ReturnsTrackedTarget()
        {
            var target = await SeedAsync(CreateTarget(branchId: 5, year: 2026, month: 3));

            var result = await _repository.GetByBranchAndMonthForUpdateAsync(5, 2026, 3);

            result.Should().NotBeNull();
            result!.Id.Should().Be(target.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchAndMonthForUpdateAsync_NoTarget_ReturnsNull()
        {
            var result = await _repository.GetByBranchAndMonthForUpdateAsync(999, 2026, 1);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidTarget_PersistsToDatabase()
        {
            var target = CreateTarget(branchId: 7, year: 2026, month: 5);

            await _repository.AddAsync(target);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<RevenueTarget>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7 && x.ReferenceMonth == 5);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
