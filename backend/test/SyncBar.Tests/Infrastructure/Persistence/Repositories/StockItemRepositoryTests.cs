using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class StockItemRepositoryTests : RepositoryTestBase
    {
        private readonly StockItemRepository _repository;

        public StockItemRepositoryTests()
        {
            _repository = new StockItemRepository(Context);
        }

        private static StockItem CreateStockItem(
            long branchId = 1, long productId = 1, decimal minimumQuantity = 10m, decimal? maximumQuantity = null) =>
            StockItem.Create(branchId, productId, minimumQuantity, maximumQuantity).Value;

        private async Task<StockItem> SeedAsync(StockItem item)
        {
            await Context.AddAsync(item);
            await Context.SaveChangesAsync();
            Context.Entry(item).State = EntityState.Detached;
            return item;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedItem()
        {
            var item = await SeedAsync(CreateStockItem());

            var result = await _repository.GetByIdAsync(item.Id);

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
            var item = await SeedAsync(CreateStockItem(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(item.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedItem()
        {
            var item = await SeedAsync(CreateStockItem());

            var result = await _repository.GetByIdForUpdateAsync(item.Id);

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
        public async Task GetByBranchAsync_MultipleActiveItems_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateStockItem(branchId: 5, productId: 1));
            var second = await SeedAsync(CreateStockItem(branchId: 5, productId: 2));
            await SeedAsync(CreateStockItem(branchId: 6, productId: 3));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveItem_IsExcluded()
        {
            var item = await SeedAsync(CreateStockItem(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(item.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoItemsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndProductForUpdateAsync_ExistingActiveItem_ReturnsTrackedItem()
        {
            var item = await SeedAsync(CreateStockItem(branchId: 5, productId: 10));

            var result = await _repository.GetByBranchAndProductForUpdateAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(item.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchAndProductForUpdateAsync_NoMatch_ReturnsNull()
        {
            var result = await _repository.GetByBranchAndProductForUpdateAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetBelowMinimumAsync_ItemBelowMinimum_ReturnsItem()
        {
            var item = await SeedAsync(CreateStockItem(branchId: 5, minimumQuantity: 10m));
            var tracked = await _repository.GetByIdForUpdateAsync(item.Id);
            tracked!.Increase(5m);
            await Context.SaveChangesAsync();

            var result = await _repository.GetBelowMinimumAsync(5);

            result.Should().ContainSingle(x => x.Id == item.Id);
        }

        [Fact]
        public async Task GetBelowMinimumAsync_ItemAboveMinimum_ReturnsEmptyList()
        {
            var item = await SeedAsync(CreateStockItem(branchId: 5, minimumQuantity: 10m));
            var tracked = await _repository.GetByIdForUpdateAsync(item.Id);
            tracked!.Increase(20m);
            await Context.SaveChangesAsync();

            var result = await _repository.GetBelowMinimumAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidItem_PersistsToDatabase()
        {
            var item = CreateStockItem(branchId: 7, productId: 77);

            await _repository.AddAsync(item);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<StockItem>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == 77);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
