using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodOrderRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodOrderRepository _repository;

        public IfoodOrderRepositoryTests()
        {
            _repository = new IfoodOrderRepository(Context);
        }

        private static IfoodOrder CreateOrder(long branchId = 1, string ifoodOrderId = "order-1") =>
            IfoodOrder.Create(1, branchId, ifoodOrderId, null, "merchant-1", "DELIVERY", null, "IMMEDIATE", null, DateTime.Now, false).Value;

        private async Task<IfoodOrder> SeedAsync(IfoodOrder order)
        {
            await Context.AddAsync(order);
            await Context.SaveChangesAsync();
            Context.Entry(order).State = EntityState.Detached;
            return order;
        }

        [Fact]
        public async Task GetByIfoodOrderIdAsync_ExistingOrder_ReturnsUntrackedOrder()
        {
            var order = await SeedAsync(CreateOrder(ifoodOrderId: "order-abc"));

            var result = await _repository.GetByIfoodOrderIdAsync("order-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIfoodOrderIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodOrderIdAsync("order-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIfoodOrderIdForUpdateAsync_ExistingOrder_ReturnsTrackedOrder()
        {
            var order = await SeedAsync(CreateOrder(ifoodOrderId: "order-abc"));

            var result = await _repository.GetByIfoodOrderIdForUpdateAsync("order-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIfoodOrderIdForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodOrderIdForUpdateAsync("order-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingOrder_ReturnsTrackedOrder()
        {
            var order = await SeedAsync(CreateOrder());

            var result = await _repository.GetByIdForUpdateAsync(order.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOpenByBranchAsync_PlacedOrder_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(branchId: 5));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().ContainSingle(x => x.Id == order.Id);
        }

        [Fact]
        public async Task GetOpenByBranchAsync_ConcludedOrder_IsExcluded()
        {
            var order = await SeedAsync(CreateOrder(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(order.Id);
            tracked!.SetStatus(IfoodOrderStatuses.Concluded, DateTime.Now);
            await Context.SaveChangesAsync();

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOpenByBranchAsync_CancelledOrder_IsExcluded()
        {
            var order = await SeedAsync(CreateOrder(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(order.Id);
            tracked!.SetStatus(IfoodOrderStatuses.Cancelled, DateTime.Now);
            await Context.SaveChangesAsync();

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOpenByBranchAsync_NoOrdersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetOpenByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidOrder_PersistsToDatabase()
        {
            var order = CreateOrder(branchId: 7, ifoodOrderId: "order-added");

            await _repository.AddAsync(order);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodOrderId == "order-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
