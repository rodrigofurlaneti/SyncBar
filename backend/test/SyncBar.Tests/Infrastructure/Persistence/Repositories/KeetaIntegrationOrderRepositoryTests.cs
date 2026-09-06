using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationOrderRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationOrderRepository _repository;

        public KeetaIntegrationOrderRepositoryTests()
        {
            _repository = new KeetaIntegrationOrderRepository(Context);
        }

        private static KeetaIntegrationOrder CreateOrder(
            long companyId = 1, long branchId = 1, long customerId = 1, long customerOrderId = 1,
            string keetaOrderId = "korder-1", string displayId = "#1", long keetaMerchantId = 100) =>
            KeetaIntegrationOrder.Create(
                companyId, branchId, customerId, customerOrderId, keetaOrderId, displayId,
                "im-1", keetaMerchantId, "DELIVERY", "MERCHANT", 50m, "{}", DateTime.UtcNow).Value;

        private async Task<KeetaIntegrationOrder> SeedAsync(KeetaIntegrationOrder order)
        {
            await Context.AddAsync(order);
            await Context.SaveChangesAsync();
            Context.Entry(order).State = EntityState.Detached;
            return order;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedOrder()
        {
            var order = await SeedAsync(CreateOrder());

            var result = await _repository.GetByIdAsync(order.Id);

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
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedOrder()
        {
            var order = await SeedAsync(CreateOrder());

            var result = await _repository.GetByIdForUpdateAsync(order.Id);

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
        public async Task GetByKeetaOrderIdAsync_ExistingId_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(keetaOrderId: "korder-abc"));

            var result = await _repository.GetByKeetaOrderIdAsync("korder-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
        }

        [Fact]
        public async Task GetByKeetaOrderIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByKeetaOrderIdAsync("korder-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByDisplayIdAsync_ExistingId_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(displayId: "#42"));

            var result = await _repository.GetByDisplayIdAsync("#42");

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
        }

        [Fact]
        public async Task GetByDisplayIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByDisplayIdAsync("#missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_MultipleOrders_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateOrder(companyId: 5, keetaOrderId: "korder-1"));
            var second = await SeedAsync(CreateOrder(companyId: 5, keetaOrderId: "korder-2"));
            await SeedAsync(CreateOrder(companyId: 6, keetaOrderId: "korder-3"));

            var result = await _repository.GetAllByCompanyIdAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_NoOrdersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByCompanyIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByBranchIdAsync_MultipleOrders_ReturnsAllForBranch()
        {
            var order = await SeedAsync(CreateOrder(branchId: 10, keetaOrderId: "korder-1"));
            await SeedAsync(CreateOrder(branchId: 20, keetaOrderId: "korder-2"));

            var result = await _repository.GetAllByBranchIdAsync(10);

            result.Should().ContainSingle(x => x.Id == order.Id);
        }

        [Fact]
        public async Task GetAllByBranchIdAsync_NoOrdersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActiveOrdersByBranchAsync_MixOfStatuses_ExcludesConcludedAndCancelled()
        {
            var created = await SeedAsync(CreateOrder(branchId: 10, keetaOrderId: "korder-created"));
            var concluded = await SeedAsync(CreateOrder(branchId: 10, keetaOrderId: "korder-concluded"));
            var trackedConcluded = await _repository.GetByIdForUpdateAsync(concluded.Id);
            trackedConcluded!.MarkAsConcluded();
            await Context.SaveChangesAsync();

            var result = await _repository.GetActiveOrdersByBranchAsync(10);

            result.Should().ContainSingle(x => x.Id == created.Id);
        }

        [Fact]
        public async Task GetActiveOrdersByBranchAsync_NoActiveOrders_ReturnsEmptyList()
        {
            var result = await _repository.GetActiveOrdersByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByKeetaOrderIdAsync_Existing_ReturnsTrue()
        {
            await SeedAsync(CreateOrder(keetaOrderId: "korder-exists"));

            var result = await _repository.ExistsByKeetaOrderIdAsync("korder-exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByKeetaOrderIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByKeetaOrderIdAsync("korder-missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidOrder_PersistsToDatabase()
        {
            var order = CreateOrder(keetaOrderId: "korder-added");

            await _repository.AddAsync(order);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.KeetaOrderId == "korder-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingOrder_PersistsChanges()
        {
            var order = await SeedAsync(CreateOrder());
            var tracked = await _repository.GetByIdForUpdateAsync(order.Id);
            tracked!.MarkAsConfirmed();

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == order.Id);

            persisted.Status.Should().Be("CONFIRMED");
            persisted.ConfirmedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_ExistingOrder_RemovesFromDatabase()
        {
            var order = await SeedAsync(CreateOrder());
            var tracked = await _repository.GetByIdForUpdateAsync(order.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == order.Id);

            persisted.Should().BeNull();
        }
    }
}
