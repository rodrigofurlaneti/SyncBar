using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ShiftClosingRepositoryTests : RepositoryTestBase
    {
        private readonly ShiftClosingRepository _repository;

        public ShiftClosingRepositoryTests()
        {
            _repository = new ShiftClosingRepository(Context);
        }

        private static ShiftClosing CreateShiftClosing(long branchId = 1) =>
            ShiftClosing.Open(branchId, openedByEmployeeId: 1).Value;

        private async Task<ShiftClosing> SeedAsync(ShiftClosing shiftClosing)
        {
            await Context.AddAsync(shiftClosing);
            await Context.SaveChangesAsync();
            Context.Entry(shiftClosing).State = EntityState.Detached;
            return shiftClosing;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedShiftClosing()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing());

            var result = await _repository.GetByIdAsync(shiftClosing.Id);

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
        public async Task GetByIdAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            var shiftClosing = await SeedAsync(CreateShiftClosing(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(shiftClosing.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedShiftClosing()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing());

            var result = await _repository.GetByIdForUpdateAsync(shiftClosing.Id);

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
        public async Task GetOpenByBranchAsync_OpenShiftExists_ReturnsShiftClosing()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing(branchId: 5));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(shiftClosing.Id);
        }

        [Fact]
        public async Task GetOpenByBranchAsync_NoOpenShift_ReturnsNull()
        {
            var result = await _repository.GetOpenByBranchAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetHistoryByBranchAsync_ShiftsWithinPeriod_ReturnsMatchingShifts()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing(branchId: 8));
            var from = shiftClosing.PeriodStart.AddDays(-1);
            var to = shiftClosing.PeriodStart.AddDays(1);

            var result = await _repository.GetHistoryByBranchAsync(8, from, to);

            result.Should().ContainSingle(x => x.Id == shiftClosing.Id);
        }

        [Fact]
        public async Task GetHistoryByBranchAsync_ShiftOutsidePeriod_ReturnsEmptyList()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing(branchId: 9));
            var from = shiftClosing.PeriodStart.AddDays(1);
            var to = shiftClosing.PeriodStart.AddDays(2);

            var result = await _repository.GetHistoryByBranchAsync(9, from, to);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetHistoryByBranchAsync_DifferentBranch_ReturnsEmptyList()
        {
            var shiftClosing = await SeedAsync(CreateShiftClosing(branchId: 10));
            var from = shiftClosing.PeriodStart.AddDays(-1);
            var to = shiftClosing.PeriodStart.AddDays(1);

            var result = await _repository.GetHistoryByBranchAsync(11, from, to);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidShiftClosing_PersistsToDatabase()
        {
            var shiftClosing = CreateShiftClosing(branchId: 7);

            await _repository.AddAsync(shiftClosing);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ShiftClosing>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
