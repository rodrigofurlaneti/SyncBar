using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodShippingDeliveryRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodShippingDeliveryRepository _repository;

        public IfoodShippingDeliveryRepositoryTests()
        {
            _repository = new IfoodShippingDeliveryRepository(Context);
        }

        private static IfoodShippingDelivery CreateDelivery(
            long branchId = 1, DateTime? requestedAt = null, string ifoodDeliveryId = "delivery-1") =>
            IfoodShippingDelivery.Create(
                branchId, "order-ref-1", "Cliente Teste", "11", "999998888",
                "01310000", "Av. Paulista", "1000", null, "Bela Vista",
                "São Paulo", "SP", "BR", null, null, null,
                15m, "quote-1", ifoodDeliveryId, null, requestedAt ?? DateTime.Now).Value;

        private async Task<IfoodShippingDelivery> SeedAsync(IfoodShippingDelivery delivery)
        {
            await Context.AddAsync(delivery);
            await Context.SaveChangesAsync();
            Context.Entry(delivery).State = EntityState.Detached;
            return delivery;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedDelivery()
        {
            var delivery = await SeedAsync(CreateDelivery());

            var result = await _repository.GetByIdAsync(delivery.Id);

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
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedDelivery()
        {
            var delivery = await SeedAsync(CreateDelivery());

            var result = await _repository.GetByIdForUpdateAsync(delivery.Id);

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
        public async Task GetOpenByBranchAsync_ActiveDelivery_ReturnsOrderedByRequestedAtDescending()
        {
            var older = await SeedAsync(CreateDelivery(branchId: 5, requestedAt: DateTime.Now.AddMinutes(-10), ifoodDeliveryId: "delivery-old"));
            var newer = await SeedAsync(CreateDelivery(branchId: 5, requestedAt: DateTime.Now, ifoodDeliveryId: "delivery-new"));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(newer.Id);
            result.ElementAt(1).Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetOpenByBranchAsync_CancelledDelivery_IsExcluded()
        {
            var delivery = await SeedAsync(CreateDelivery(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(delivery.Id);
            tracked!.MarkCancelled(reason: null, DateTime.Now);
            await Context.SaveChangesAsync();

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
            var delivery = CreateDelivery(branchId: 7);

            await _repository.AddAsync(delivery);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodShippingDelivery>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
