using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodFinancialEventRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodFinancialEventRepository _repository;

        public IfoodFinancialEventRepositoryTests()
        {
            _repository = new IfoodFinancialEventRepository(Context);
        }

        private static IfoodFinancialEvent CreateEvent(
            long branchId = 1, string ifoodEventId = "evt-1", DateTime? competenceDate = null) =>
            IfoodFinancialEvent.Create(
                branchId, ifoodEventId, "Comissão", null, null, 10m, true,
                competenceDate ?? DateTime.Now, DateTime.Now.AddDays(-15), DateTime.Now.AddDays(15),
                null, null, null, "{}").Value;

        private async Task<IfoodFinancialEvent> SeedAsync(IfoodFinancialEvent financialEvent)
        {
            await Context.AddAsync(financialEvent);
            await Context.SaveChangesAsync();
            Context.Entry(financialEvent).State = EntityState.Detached;
            return financialEvent;
        }

        [Fact]
        public async Task ExistsByIfoodEventIdAsync_ExistingEvent_ReturnsTrue()
        {
            await SeedAsync(CreateEvent(branchId: 5, ifoodEventId: "evt-exists"));

            var result = await _repository.ExistsByIfoodEventIdAsync(5, "evt-exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByIfoodEventIdAsync_DifferentBranch_ReturnsFalse()
        {
            await SeedAsync(CreateEvent(branchId: 5, ifoodEventId: "evt-exists"));

            var result = await _repository.ExistsByIfoodEventIdAsync(6, "evt-exists");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByIfoodEventIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByIfoodEventIdAsync(999, "evt-missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_EventWithinPeriod_ReturnsOrderedByCompetenceDate()
        {
            var later = await SeedAsync(CreateEvent(branchId: 5, ifoodEventId: "evt-later", competenceDate: DateTime.Now.AddDays(2)));
            var earlier = await SeedAsync(CreateEvent(branchId: 5, ifoodEventId: "evt-earlier", competenceDate: DateTime.Now.AddDays(1)));

            var result = await _repository.GetByBranchAndPeriodAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(earlier.Id);
            result.ElementAt(1).Id.Should().Be(later.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OutsidePeriod_ReturnsEmptyList()
        {
            await SeedAsync(CreateEvent(branchId: 5, competenceDate: DateTime.Now.AddDays(20)));

            var result = await _repository.GetByBranchAndPeriodAsync(5, DateTime.Now, DateTime.Now.AddDays(10));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidEvent_PersistsToDatabase()
        {
            var financialEvent = CreateEvent(branchId: 7, ifoodEventId: "evt-added");

            await _repository.AddAsync(financialEvent);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodFinancialEvent>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodEventId == "evt-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
