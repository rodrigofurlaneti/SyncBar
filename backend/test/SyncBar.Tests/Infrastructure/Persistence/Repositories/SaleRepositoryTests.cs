using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class SaleRepositoryTests : RepositoryTestBase
    {
        private readonly SaleRepository _repository;

        public SaleRepositoryTests()
        {
            _repository = new SaleRepository(Context);
        }

        private static Sale CreateSale(
            long branchId = 1, long customerOrderId = 1, long cashSessionId = 1, long saleNumber = 1) =>
            Sale.Create(branchId, customerOrderId, cashSessionId, employeeId: 1, saleNumber, 100m, 0m, 0m).Value;

        private async Task<Sale> SeedAsync(Sale sale)
        {
            await Context.AddAsync(sale);
            await Context.SaveChangesAsync();
            Context.Entry(sale).State = EntityState.Detached;
            return sale;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedSaleWithPayments()
        {
            var sale = CreateSale();
            sale.AddPayment(paymentMethodId: 1, amount: 100m, changeAmount: null, authorizationCode: null, allowsChange: false);
            await SeedAsync(sale);

            var result = await _repository.GetByIdAsync(sale.Id);

            result.Should().NotBeNull();
            result!.Payments.Should().ContainSingle();
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
            var sale = await SeedAsync(CreateSale(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(sale.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedSale()
        {
            var sale = await SeedAsync(CreateSale());

            var result = await _repository.GetByIdForUpdateAsync(sale.Id);

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
        public async Task GetByCashSessionAsync_MultipleActiveSales_ReturnsAllForSession()
        {
            var first = await SeedAsync(CreateSale(customerOrderId: 1, cashSessionId: 5, saleNumber: 1));
            var second = await SeedAsync(CreateSale(customerOrderId: 2, cashSessionId: 5, saleNumber: 2));
            await SeedAsync(CreateSale(customerOrderId: 3, cashSessionId: 6, saleNumber: 3));

            var result = await _repository.GetByCashSessionAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCashSessionAsync_NoSalesForSession_ReturnsEmptyList()
        {
            var result = await _repository.GetByCashSessionAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_SaleWithinPeriod_ReturnsSale()
        {
            var sale = await SeedAsync(CreateSale(branchId: 5));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

            result.Should().ContainSingle(x => x.Id == sale.Id);
        }

        [Fact]
        public async Task GetByBranchAndPeriodAsync_OutsidePeriod_ReturnsEmptyList()
        {
            await SeedAsync(CreateSale(branchId: 5));

            var result = await _repository.GetByBranchAndPeriodAsync(
                5, DateTime.Now.AddDays(10), DateTime.Now.AddDays(20));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetNextSaleNumberAsync_ExistingSales_ReturnsMaxPlusOne()
        {
            await SeedAsync(CreateSale(branchId: 5, saleNumber: 10));

            var result = await _repository.GetNextSaleNumberAsync(5);

            result.Should().Be(11);
        }

        [Fact]
        public async Task GetNextSaleNumberAsync_NoSalesForBranch_ReturnsOne()
        {
            var result = await _repository.GetNextSaleNumberAsync(999);

            result.Should().Be(1);
        }

        [Fact]
        public async Task ExistsActiveByOrderAsync_ActiveSaleExists_ReturnsTrue()
        {
            await SeedAsync(CreateSale(customerOrderId: 42));

            var result = await _repository.ExistsActiveByOrderAsync(42);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsActiveByOrderAsync_NoSaleForOrder_ReturnsFalse()
        {
            var result = await _repository.ExistsActiveByOrderAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetActiveByCustomerOrderIdAsync_ActiveSaleExists_ReturnsUntrackedSaleWithPayments()
        {
            var sale = CreateSale(customerOrderId: 42);
            sale.AddPayment(paymentMethodId: 1, amount: 100m, changeAmount: null, authorizationCode: null, allowsChange: false);
            await SeedAsync(sale);

            var result = await _repository.GetActiveByCustomerOrderIdAsync(42);

            result.Should().NotBeNull();
            result!.Payments.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetActiveByCustomerOrderIdAsync_NoSaleForOrder_ReturnsNull()
        {
            var result = await _repository.GetActiveByCustomerOrderIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveByCustomerOrderIdAsync_SaleIsInactive_ReturnsNull()
        {
            var sale = CreateSale(customerOrderId: 42);
            sale.Deactivate();
            await SeedAsync(sale);

            var result = await _repository.GetActiveByCustomerOrderIdAsync(42);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidSale_PersistsToDatabase()
        {
            var sale = CreateSale(branchId: 7, saleNumber: 99);

            await _repository.AddAsync(sale);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Sale>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SaleNumber == 99);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
