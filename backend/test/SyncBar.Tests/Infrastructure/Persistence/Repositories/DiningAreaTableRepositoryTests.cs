using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class DiningAreaTableRepositoryTests : RepositoryTestBase
    {
        private readonly DiningAreaTableRepository _repository;

        public DiningAreaTableRepositoryTests()
        {
            _repository = new DiningAreaTableRepository(Context);
        }

        private static DiningAreaTable CreateLink(long diningAreaId = 1, long diningTableId = 1) =>
            DiningAreaTable.Create(diningAreaId, diningTableId).Value;

        private async Task<DiningAreaTable> SeedAsync(DiningAreaTable link)
        {
            await Context.AddAsync(link);
            await Context.SaveChangesAsync();
            Context.Entry(link).State = EntityState.Detached;
            return link;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsTrackedLink()
        {
            var link = await SeedAsync(CreateLink());

            var result = await _repository.GetByIdAsync(link.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(link.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByDiningAreaIdAsync_MultipleActiveLinks_ReturnsAllForArea()
        {
            var first = await SeedAsync(CreateLink(diningAreaId: 5, diningTableId: 1));
            var second = await SeedAsync(CreateLink(diningAreaId: 5, diningTableId: 2));
            await SeedAsync(CreateLink(diningAreaId: 6, diningTableId: 3));

            var result = (await _repository.GetByDiningAreaIdAsync(5)).ToList();

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByDiningAreaIdAsync_NoLinksForArea_ReturnsEmptyList()
        {
            var result = await _repository.GetByDiningAreaIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByTableIdAsync_ActiveLinkExists_ReturnsTrue()
        {
            await SeedAsync(CreateLink(diningTableId: 10));

            var result = await _repository.ExistsByTableIdAsync(10);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByTableIdAsync_NoLink_ReturnsFalse()
        {
            var result = await _repository.ExistsByTableIdAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByTableIdAsync_ActiveLinkExists_ReturnsLink()
        {
            var link = await SeedAsync(CreateLink(diningTableId: 10));

            var result = await _repository.GetByTableIdAsync(10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(link.Id);
        }

        [Fact]
        public async Task GetByTableIdAsync_NoLink_ReturnsNull()
        {
            var result = await _repository.GetByTableIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidLink_PersistsToDatabase()
        {
            var link = CreateLink(diningAreaId: 20, diningTableId: 20);

            await _repository.AddAsync(link);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningAreaTable>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DiningAreaId == 20 && x.DiningTableId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingLink_PersistsChanges()
        {
            var link = await SeedAsync(CreateLink());
            var tracked = await Context.Set<DiningAreaTable>().FirstAsync(x => x.Id == link.Id);
            tracked.Deactivate();

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningAreaTable>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == link.Id);

            persisted.IsActive.Should().BeFalse();
        }
    }
}
