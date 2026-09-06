using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class DiningAreaRepositoryTests : RepositoryTestBase
    {
        private readonly DiningAreaRepository _repository;

        public DiningAreaRepositoryTests()
        {
            _repository = new DiningAreaRepository(Context);
        }

        private static DiningArea CreateArea(long branchId = 1, string name = "Salão Principal") =>
            DiningArea.Create(branchId, name).Value;

        private async Task<DiningArea> SeedAsync(DiningArea area)
        {
            await Context.AddAsync(area);
            await Context.SaveChangesAsync();
            Context.Entry(area).State = EntityState.Detached;
            return area;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsTrackedArea()
        {
            var area = await SeedAsync(CreateArea());

            var result = await _repository.GetByIdAsync(area.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(area.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchIdAsync_MultipleActiveAreas_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateArea(branchId: 5, name: "A1"));
            var second = await SeedAsync(CreateArea(branchId: 5, name: "A2"));
            await SeedAsync(CreateArea(branchId: 6, name: "A3"));

            var result = (await _repository.GetByBranchIdAsync(5)).ToList();

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchIdAsync_InactiveArea_IsExcluded()
        {
            var area = await SeedAsync(CreateArea(branchId: 5));
            var tracked = await Context.Set<DiningArea>().FirstAsync(x => x.Id == area.Id);
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchIdAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchIdAsync_NoAreasForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidArea_PersistsToDatabase()
        {
            var area = CreateArea(branchId: 7, name: "Nova Área");

            await _repository.AddAsync(area);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningArea>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Nova Área");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingArea_PersistsChanges()
        {
            var area = await SeedAsync(CreateArea());
            var tracked = await Context.Set<DiningArea>().FirstAsync(x => x.Id == area.Id);
            tracked.UpdateName("Nome Atualizado");

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningArea>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == area.Id);

            persisted.Name.Should().Be("Nome Atualizado");
        }
    }
}
