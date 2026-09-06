using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ProductStockRepositoryTests : RepositoryTestBase
    {
        private readonly ProductStockRepository _repository;

        public ProductStockRepositoryTests()
        {
            _repository = new ProductStockRepository(Context);
        }

        [Fact]
        public async Task GetByProductIdAsync_ExistingStock_ReturnsTrackedStock()
        {
            var stock = new ProductStock(productId: 5, initialBalance: 100m, minimumQuantity: 10m);
            await Context.AddAsync(stock);
            await Context.SaveChangesAsync();
            Context.Entry(stock).State = EntityState.Detached;

            var result = await _repository.GetByProductIdAsync(5);

            result.Should().NotBeNull();
            result!.ProductId.Should().Be(5);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByProductIdAsync_NonExistingProduct_ReturnsNull()
        {
            var result = await _repository.GetByProductIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task Add_ValidStock_PersistsToDatabase()
        {
            var stock = new ProductStock(productId: 7, initialBalance: 50m, minimumQuantity: 5m);

            _repository.Add(stock);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ProductStock>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == 7);

            persisted.Should().NotBeNull();
            persisted!.CurrentBalance.Should().Be(50m);
        }

        [Fact]
        public async Task AddMovement_ValidMovement_PersistsToDatabase()
        {
            var stockItem = StockItem.Create(branchId: 1, productId: 7, minimumQuantity: 0m, maximumQuantity: null).Value;
            await Context.AddAsync(stockItem);
            await Context.SaveChangesAsync();

            var movement = StockMovement.Create(
                stockItem.Id, stockMovementTypeId: 1, purchaseItemId: null, orderItemId: null, employeeId: 1,
                quantity: 10m, unitCost: 1m, totalCost: 10m, documentNumber: null, movedAt: DateTime.Now, notes: null).Value;

            _repository.AddMovement(movement);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<StockMovement>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StockItemId == stockItem.Id);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
