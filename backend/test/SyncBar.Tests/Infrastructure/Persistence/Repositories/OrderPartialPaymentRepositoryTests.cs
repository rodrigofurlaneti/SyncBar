using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class OrderPartialPaymentRepositoryTests : RepositoryTestBase
    {
        private readonly OrderPartialPaymentRepository _repository;

        public OrderPartialPaymentRepositoryTests()
        {
            _repository = new OrderPartialPaymentRepository(Context);
        }

        private static OrderPartialPayment CreatePayment(long customerOrderId = 1, long cashSessionId = 1) =>
            OrderPartialPayment.Create(customerOrderId, cashSessionId, paymentMethodId: 1, employeeId: 1, amount: 50m, authorizationCode: null, payerName: null).Value;

        private async Task<OrderPartialPayment> SeedAsync(OrderPartialPayment payment)
        {
            await Context.AddAsync(payment);
            await Context.SaveChangesAsync();
            Context.Entry(payment).State = EntityState.Detached;
            return payment;
        }

        [Fact]
        public async Task GetByOrderAsync_MultipleActivePayments_ReturnsAllForOrder()
        {
            var first = await SeedAsync(CreatePayment(customerOrderId: 5));
            var second = await SeedAsync(CreatePayment(customerOrderId: 5));
            await SeedAsync(CreatePayment(customerOrderId: 6));

            var result = await _repository.GetByOrderAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByOrderAsync_InactivePayment_IsExcluded()
        {
            var payment = await SeedAsync(CreatePayment(customerOrderId: 5));
            payment.Deactivate();
            Context.Update(payment);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByOrderAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByOrderAsync_NoPaymentsForOrder_ReturnsEmptyList()
        {
            var result = await _repository.GetByOrderAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCashSessionAsync_MultipleActivePayments_ReturnsAllForSession()
        {
            var first = await SeedAsync(CreatePayment(cashSessionId: 5));
            var second = await SeedAsync(CreatePayment(cashSessionId: 5));
            await SeedAsync(CreatePayment(cashSessionId: 6));

            var result = await _repository.GetByCashSessionAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCashSessionAsync_NoPaymentsForSession_ReturnsEmptyList()
        {
            var result = await _repository.GetByCashSessionAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidPayment_PersistsToDatabase()
        {
            var payment = CreatePayment(customerOrderId: 7);

            await _repository.AddAsync(payment);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<OrderPartialPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerOrderId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
