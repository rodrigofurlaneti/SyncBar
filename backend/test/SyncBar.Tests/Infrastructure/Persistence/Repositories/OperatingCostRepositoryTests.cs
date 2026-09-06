using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class OperatingCostRepositoryTests : RepositoryTestBase
    {
        private readonly OperatingCostRepository _repository;

        public OperatingCostRepositoryTests()
        {
            _repository = new OperatingCostRepository(Context);
        }

        private static OperatingCost CreateCost(long branchId = 1, int year = 2026, int month = 1) =>
            OperatingCost.Create(branchId, costTypeId: 1, description: "Aluguel", amount: 2000m, referenceYear: year, referenceMonth: month).Value;

        private async Task<OperatingCost> SeedAsync(OperatingCost cost)
        {
            await Context.AddAsync(cost);
            await Context.SaveChangesAsync();
            Context.Entry(cost).State = EntityState.Detached;
            return cost;
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedCost()
        {
            var cost = await SeedAsync(CreateCost());

            var result = await _repository.GetByIdForUpdateAsync(cost.Id);

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
        public async Task GetByBranchAndMonthAsync_MultipleActiveCosts_ReturnsAllForBranchAndMonth()
        {
            var first = await SeedAsync(CreateCost(branchId: 5, year: 2026, month: 3));
            var second = await SeedAsync(CreateCost(branchId: 5, year: 2026, month: 3));
            await SeedAsync(CreateCost(branchId: 5, year: 2026, month: 4));

            var result = await _repository.GetByBranchAndMonthAsync(5, 2026, 3);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAndMonthAsync_InactiveCost_IsExcluded()
        {
            var cost = await SeedAsync(CreateCost(branchId: 5, year: 2026, month: 3));
            var tracked = await _repository.GetByIdForUpdateAsync(cost.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAndMonthAsync(5, 2026, 3);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndMonthAsync_NoCostsForBranchAndMonth_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAndMonthAsync(999, 2026, 1);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidCost_PersistsToDatabase()
        {
            var cost = CreateCost(branchId: 7);

            await _repository.AddAsync(cost);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<OperatingCost>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
