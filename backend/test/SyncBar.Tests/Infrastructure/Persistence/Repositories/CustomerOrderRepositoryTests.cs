using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CustomerOrderRepositoryTests : RepositoryTestBase
    {
        private readonly CustomerOrderRepository _repository;

        public CustomerOrderRepositoryTests()
        {
            _repository = new CustomerOrderRepository(Context);
        }

        private static CustomerOrder CreateOrder(
            long branchId = 1, long? diningTableId = 1, long? comandaId = null, DateTime? openedAt = null) =>
            CustomerOrder.Create(
                branchId, diningTableId, comandaId, employeeId: 1, guestCount: 2, notes: null,
                Now: openedAt ?? DateTime.UtcNow).Value;

        private async Task<CustomerOrder> SeedAsync(CustomerOrder order)
        {
            await Context.AddAsync(order);
            await Context.SaveChangesAsync();
            Context.Entry(order).State = EntityState.Detached;
            return order;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedOrderWithItems()
        {
            var order = await SeedAsync(CreateOrder());

            var result = await _repository.GetByIdAsync(order.Id);

            result.Should().NotBeNull();
            result!.Items.Should().BeEmpty();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
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
            var order = await SeedAsync(CreateOrder(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(order.Id);

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
        public async Task GetOpenByBranchAsync_OpenOrder_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(branchId: 5));

            var result = await _repository.GetOpenByBranchAsync(5);

            result.Should().ContainSingle(x => x.Id == order.Id);
        }

        [Fact]
        public async Task GetOpenByBranchAsync_NoOpenOrdersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetOpenByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_MatchingIds_ReturnsOrders()
        {
            var first = await SeedAsync(CreateOrder(diningTableId: 1));
            var second = await SeedAsync(CreateOrder(diningTableId: 2));
            await SeedAsync(CreateOrder(diningTableId: 3));

            var result = await _repository.GetByIdsAsync([first.Id, second.Id]);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdsAsync_NoMatchingIds_ReturnsEmptyList()
        {
            var result = await _repository.GetByIdsAsync([999]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OrderWithinPeriod_ReturnsOrder()
        {
            var openedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            var order = await SeedAsync(CreateOrder(branchId: 5, openedAt: openedAt));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

            result.Should().ContainSingle(x => x.Id == order.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OrderOutsidePeriod_ReturnsEmptyList()
        {
            var openedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            await SeedAsync(CreateOrder(branchId: 5, openedAt: openedAt));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, new DateTime(2026, 3, 1), new DateTime(2026, 4, 1));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task HasOpenOrderForTableAsync_OpenOrderExists_ReturnsTrue()
        {
            await SeedAsync(CreateOrder(diningTableId: 10));

            var result = await _repository.HasOpenOrderForTableAsync(10);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasOpenOrderForTableAsync_NoOpenOrder_ReturnsFalse()
        {
            var result = await _repository.HasOpenOrderForTableAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOpenByTableForUpdateAsync_OpenOrderExists_ReturnsTrackedOrder()
        {
            var order = await SeedAsync(CreateOrder(diningTableId: 10));

            var result = await _repository.GetOpenByTableForUpdateAsync(10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetOpenByTableForUpdateAsync_NoOpenOrder_ReturnsNull()
        {
            var result = await _repository.GetOpenByTableForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOpenByComandaForUpdateAsync_OpenOrderExists_ReturnsTrackedOrder()
        {
            var order = await SeedAsync(CreateOrder(diningTableId: null, comandaId: 20));

            var result = await _repository.GetOpenByComandaForUpdateAsync(20);

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetOpenByComandaForUpdateAsync_NoOpenOrder_ReturnsNull()
        {
            var result = await _repository.GetOpenByComandaForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task HasOpenOrderForComandaAsync_OpenOrderExists_ReturnsTrue()
        {
            await SeedAsync(CreateOrder(diningTableId: null, comandaId: 20));

            var result = await _repository.HasOpenOrderForComandaAsync(20);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasOpenOrderForComandaAsync_NoOpenOrder_ReturnsFalse()
        {
            var result = await _repository.HasOpenOrderForComandaAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOpenByComandaAsync_OrderNotClosed_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(diningTableId: null, comandaId: 30));

            var result = await _repository.GetOpenByComandaAsync(30);

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
        }

        [Fact]
        public async Task GetOpenByComandaAsync_NoOrder_ReturnsNull()
        {
            var result = await _repository.GetOpenByComandaAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetOpenByTableAsync_OrderNotClosed_ReturnsOrder()
        {
            var order = await SeedAsync(CreateOrder(diningTableId: 40));

            var result = await _repository.GetOpenByTableAsync(40);

            result.Should().NotBeNull();
            result!.Id.Should().Be(order.Id);
        }

        [Fact]
        public async Task GetOpenByTableAsync_NoOrder_ReturnsNull()
        {
            var result = await _repository.GetOpenByTableAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidOrder_PersistsToDatabase()
        {
            var order = CreateOrder(branchId: 7, diningTableId: 50);

            await _repository.AddAsync(order);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DiningTableId == 50);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
