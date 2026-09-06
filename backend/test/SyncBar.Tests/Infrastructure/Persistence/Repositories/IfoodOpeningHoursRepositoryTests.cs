using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodOpeningHoursRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodOpeningHoursRepository _repository;

        public IfoodOpeningHoursRepositoryTests()
        {
            _repository = new IfoodOpeningHoursRepository(Context);
        }

        private static IfoodOpeningHours CreateHours(long branchId = 1, int dayOfWeek = 1, TimeSpan? start = null) =>
            IfoodOpeningHours.Create(branchId, dayOfWeek, start ?? TimeSpan.FromHours(8), durationMinutes: 600).Value;

        private async Task<IfoodOpeningHours> SeedAsync(IfoodOpeningHours hours)
        {
            await Context.AddAsync(hours);
            await Context.SaveChangesAsync();
            Context.Entry(hours).State = EntityState.Detached;
            return hours;
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveEntries_ReturnsOrderedByDayThenStart()
        {
            var wednesday = await SeedAsync(CreateHours(branchId: 5, dayOfWeek: 3, start: TimeSpan.FromHours(8)));
            var mondayLate = await SeedAsync(CreateHours(branchId: 5, dayOfWeek: 1, start: TimeSpan.FromHours(18)));
            var mondayEarly = await SeedAsync(CreateHours(branchId: 5, dayOfWeek: 1, start: TimeSpan.FromHours(8)));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(3);
            result.ElementAt(0).Id.Should().Be(mondayEarly.Id);
            result.ElementAt(1).Id.Should().Be(mondayLate.Id);
            result.ElementAt(2).Id.Should().Be(wednesday.Id);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveEntry_IsExcluded()
        {
            var hours = await SeedAsync(CreateHours(branchId: 5));
            var tracked = await Context.Set<IfoodOpeningHours>().FirstAsync(x => x.Id == hours.Id);
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoEntriesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_ExistingEntries_ReturnsTrackedEntities()
        {
            var hours = await SeedAsync(CreateHours(branchId: 5));

            var result = await _repository.GetByBranchForUpdateAsync(5);

            result.Should().ContainSingle(x => x.Id == hours.Id);
            Context.Entry(result.Single()).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_NoEntries_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchForUpdateAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddRangeAsync_ValidEntries_PersistsToDatabase()
        {
            var first = CreateHours(branchId: 7, dayOfWeek: 1);
            var second = CreateHours(branchId: 7, dayOfWeek: 2);

            await _repository.AddRangeAsync([first, second]);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodOpeningHours>()
                .AsNoTracking()
                .Where(x => x.BranchId == 7)
                .ToListAsync();

            persisted.Should().HaveCount(2);
        }
    }
}
