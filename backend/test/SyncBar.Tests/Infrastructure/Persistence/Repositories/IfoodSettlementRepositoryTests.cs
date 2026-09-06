using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodSettlementRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodSettlementRepository _repository;

        public IfoodSettlementRepositoryTests()
        {
            _repository = new IfoodSettlementRepository(Context);
        }

        private static IfoodSettlement CreateSettlement(
            long branchId = 1, string ifoodSettlementId = "settle-1", DateTime? paymentDate = null) =>
            IfoodSettlement.Create(
                branchId, ifoodSettlementId, "TRANSFER", null, 100m, "SUCCEED",
                paymentDate, null, null, null, "{}").Value;

        private async Task<IfoodSettlement> SeedAsync(IfoodSettlement settlement)
        {
            await Context.AddAsync(settlement);
            await Context.SaveChangesAsync();
            Context.Entry(settlement).State = EntityState.Detached;
            return settlement;
        }

        [Fact]
        public async Task GetByIfoodSettlementIdForUpdateAsync_ExistingSettlement_ReturnsTrackedSettlement()
        {
            var settlement = await SeedAsync(CreateSettlement(branchId: 5, ifoodSettlementId: "settle-abc"));

            var result = await _repository.GetByIfoodSettlementIdForUpdateAsync(5, "settle-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(settlement.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIfoodSettlementIdForUpdateAsync_DifferentBranch_ReturnsNull()
        {
            await SeedAsync(CreateSettlement(branchId: 5, ifoodSettlementId: "settle-abc"));

            var result = await _repository.GetByIfoodSettlementIdForUpdateAsync(6, "settle-abc");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIfoodSettlementIdForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodSettlementIdForUpdateAsync(999, "settle-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_SettlementWithPaymentDateWithinPeriod_ReturnsOrderedByPaymentDate()
        {
            var later = await SeedAsync(CreateSettlement(branchId: 5, ifoodSettlementId: "settle-later", paymentDate: DateTime.Now.AddDays(2)));
            var earlier = await SeedAsync(CreateSettlement(branchId: 5, ifoodSettlementId: "settle-earlier", paymentDate: DateTime.Now.AddDays(1)));

            var result = await _repository.GetByBranchAndPeriodAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(earlier.Id);
            result.ElementAt(1).Id.Should().Be(later.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_NoPaymentDate_FallsBackToCreatedAt()
        {
            var settlement = await SeedAsync(CreateSettlement(branchId: 5, paymentDate: null));

            var result = await _repository.GetByBranchAndPeriodAsync(5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().ContainSingle(x => x.Id == settlement.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OutsidePeriod_ReturnsEmptyList()
        {
            await SeedAsync(CreateSettlement(branchId: 5, paymentDate: DateTime.Now.AddDays(20)));

            var result = await _repository.GetByBranchAndPeriodAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidSettlement_PersistsToDatabase()
        {
            var settlement = CreateSettlement(branchId: 7, ifoodSettlementId: "settle-added");

            await _repository.AddAsync(settlement);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodSettlement>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodSettlementId == "settle-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
