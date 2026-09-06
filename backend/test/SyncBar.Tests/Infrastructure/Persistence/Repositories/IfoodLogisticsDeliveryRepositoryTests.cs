using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodLogisticsDeliveryRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodLogisticsDeliveryRepository _repository;

        public IfoodLogisticsDeliveryRepositoryTests()
        {
            _repository = new IfoodLogisticsDeliveryRepository(Context);
        }

        private static IfoodLogisticsDelivery CreateDelivery(long ifoodOrderId = 1, long branchId = 1) =>
            IfoodLogisticsDelivery.Create(ifoodOrderId, branchId, "Motorista", "11999998888", "MOTO", DateTime.Now).Value;

        private static IfoodOrder CreateOrder(long branchId = 1, string ifoodOrderId = "order-1") =>
            IfoodOrder.Create(1, branchId, ifoodOrderId, null, "merchant-1", "DELIVERY", null, "IMMEDIATE", null, DateTime.Now, false).Value;

        private async Task<IfoodLogisticsDelivery> SeedAsync(IfoodLogisticsDelivery delivery)
        {
            await Context.AddAsync(delivery);
            await Context.SaveChangesAsync();
            Context.Entry(delivery).State = EntityState.Detached;
            return delivery;
        }

        [Fact]
        public async Task GetByIfoodOrderIdAsync_ExistingDelivery_ReturnsUntrackedDelivery()
        {
            var delivery = await SeedAsync(CreateDelivery(ifoodOrderId: 42));

            var result = await _repository.GetByIfoodOrderIdAsync(42);

            result.Should().NotBeNull();
            result!.Id.Should().Be(delivery.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIfoodOrderIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodOrderIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIfoodOrderIdForUpdateAsync_ExistingDelivery_ReturnsTrackedDelivery()
        {
            var delivery = await SeedAsync(CreateDelivery(ifoodOrderId: 42));

            var result = await _repository.GetByIfoodOrderIdForUpdateAsync(42);

            result.Should().NotBeNull();
            result!.Id.Should().Be(delivery.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIfoodOrderIdForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodOrderIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOpenByBranchAsync_DeliveryForOpenOrder_ReturnsDelivery()
        {
            var order = await Context.AddAsync(CreateOrder(branchId: 5, ifoodOrderId: "order-open"));
            await Context.SaveChangesAsync();
            var delivery = await SeedAsync(CreateDelivery(ifoodOrderId: order.Entity.Id, branchId: 5));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().ContainSingle(x => x.Id == delivery.Id);
        }

        [Fact]
        public async Task GetOpenByBranchAsync_DeliveryForConcludedOrder_IsExcluded()
        {
            var order = CreateOrder(branchId: 5, ifoodOrderId: "order-concluded");
            await Context.AddAsync(order);
            await Context.SaveChangesAsync();
            order.SetStatus(IfoodOrderStatuses.Concluded, DateTime.Now);
            Context.Update(order);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateDelivery(ifoodOrderId: order.Id, branchId: 5));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetOpenByBranchAsync_NoDeliveriesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetOpenByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidDelivery_PersistsToDatabase()
        {
            var delivery = CreateDelivery(ifoodOrderId: 77, branchId: 7);

            await _repository.AddAsync(delivery);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodLogisticsDelivery>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodOrderId == 77);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
