using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class StockMovementRepositoryTests : RepositoryTestBase
    {
        private readonly StockMovementRepository _repository;

        public StockMovementRepositoryTests()
        {
            _repository = new StockMovementRepository(Context);
        }

        private static StockItem CreateStockItem(long branchId = 1, long productId = 1) =>
            StockItem.Create(branchId, productId, 0m, null).Value;

        private static StockMovement CreateMovement(
            long stockItemId, long stockMovementTypeId = StockMovementTypeIds.SaidaVenda,
            decimal quantity = 1m, decimal? totalCost = 10m, DateTime? movedAt = null) =>
            StockMovement.Create(
                stockItemId, stockMovementTypeId, purchaseItemId: null, orderItemId: null, employeeId: 1,
                quantity, unitCost: totalCost, totalCost, documentNumber: null,
                movedAt: movedAt ?? DateTime.Now, notes: null).Value;

        private async Task<StockMovement> SeedAsync(StockMovement movement)
        {
            await Context.AddAsync(movement);
            await Context.SaveChangesAsync();
            Context.Entry(movement).State = EntityState.Detached;
            return movement;
        }

        [Fact]
        public async Task GetByStockItemAsync_MultipleActiveMovements_ReturnsAllForItem()
        {
            var first = await SeedAsync(CreateMovement(stockItemId: 5));
            var second = await SeedAsync(CreateMovement(stockItemId: 5));
            await SeedAsync(CreateMovement(stockItemId: 6));

            var result = await _repository.GetByStockItemAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByStockItemAsync_InactiveMovement_IsExcluded()
        {
            var movement = await SeedAsync(CreateMovement(stockItemId: 5));
            movement.Deactivate();
            Context.Update(movement);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByStockItemAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByStockItemAsync_NoMovementsForItem_ReturnsEmptyList()
        {
            var result = await _repository.GetByStockItemAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSaleCostAsync_SaleMovementsWithinPeriod_SumsTotalCost()
        {
            var item = await Context.AddAsync(CreateStockItem(branchId: 5));
            await Context.SaveChangesAsync();
            await SeedAsync(CreateMovement(item.Entity.Id, totalCost: 10m));
            await SeedAsync(CreateMovement(item.Entity.Id, totalCost: 15m));

            var result = await _repository.GetSaleCostAsync(5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().Be(25m);
        }

        [Fact]
        public async Task GetSaleCostAsync_NonSaleMovementType_IsExcluded()
        {
            var item = await Context.AddAsync(CreateStockItem(branchId: 5));
            await Context.SaveChangesAsync();
            await SeedAsync(CreateMovement(item.Entity.Id, stockMovementTypeId: 1, totalCost: 100m));

            var result = await _repository.GetSaleCostAsync(5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().Be(0m);
        }

        [Fact]
        public async Task GetSaleCostAsync_NoMovements_ReturnsZero()
        {
            var result = await _repository.GetSaleCostAsync(999, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().Be(0m);
        }

        [Fact]
        public async Task GetSaleQuantitiesByProductAsync_MultipleMovementsSameProduct_GroupsQuantities()
        {
            var item = CreateStockItem(branchId: 5, productId: 42);
            await Context.AddAsync(item);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateMovement(item.Id, quantity: 2m));
            await SeedAsync(CreateMovement(item.Id, quantity: 3m));

            var result = await _repository.GetSaleQuantitiesByProductAsync(5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().ContainSingle(x => x.ProductId == 42 && x.Quantity == 5m);
        }

        [Fact]
        public async Task GetSaleQuantitiesByProductAsync_NoMovements_ReturnsEmptyList()
        {
            var result = await _repository.GetSaleQuantitiesByProductAsync(999, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMovement_PersistsToDatabase()
        {
            var movement = CreateMovement(stockItemId: 7);

            await _repository.AddAsync(movement);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<StockMovement>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StockItemId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
