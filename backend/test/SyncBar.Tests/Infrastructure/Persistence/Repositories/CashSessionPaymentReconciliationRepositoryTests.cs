using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CashSessionPaymentReconciliationRepositoryTests : RepositoryTestBase
    {
        private readonly CashSessionPaymentReconciliationRepository _repository;

        public CashSessionPaymentReconciliationRepositoryTests()
        {
            _repository = new CashSessionPaymentReconciliationRepository(Context);
        }

        private static CashSessionPaymentReconciliation CreateReconciliation(
            long cashSessionId = 1, long paymentMethodId = 2, decimal expectedAmount = 100m, decimal countedAmount = 100m) =>
            CashSessionPaymentReconciliation.Create(cashSessionId, paymentMethodId, expectedAmount, countedAmount).Value;

        private async Task<CashSessionPaymentReconciliation> SeedAsync(CashSessionPaymentReconciliation reconciliation)
        {
            await Context.AddAsync(reconciliation);
            await Context.SaveChangesAsync();
            Context.Entry(reconciliation).State = EntityState.Detached;
            return reconciliation;
        }

        [Fact]
        public async Task GetByCashSessionAsync_MultipleActiveReconciliations_ReturnsAllForSession()
        {
            var first = await SeedAsync(CreateReconciliation(cashSessionId: 5, paymentMethodId: 2));
            var second = await SeedAsync(CreateReconciliation(cashSessionId: 5, paymentMethodId: 3));
            await SeedAsync(CreateReconciliation(cashSessionId: 6, paymentMethodId: 2));

            var result = await _repository.GetByCashSessionAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCashSessionAsync_InactiveReconciliation_IsExcluded()
        {
            var reconciliation = await SeedAsync(CreateReconciliation(cashSessionId: 5));
            reconciliation.Deactivate();
            Context.Update(reconciliation);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCashSessionAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCashSessionAsync_NoReconciliationsForSession_ReturnsEmptyList()
        {
            var result = await _repository.GetByCashSessionAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddRangeAsync_ValidReconciliations_PersistsToDatabase()
        {
            var credit = CreateReconciliation(cashSessionId: 7, paymentMethodId: 2, expectedAmount: 200m, countedAmount: 190m);
            var pix = CreateReconciliation(cashSessionId: 7, paymentMethodId: 4, expectedAmount: 50m, countedAmount: 50m);

            await _repository.AddRangeAsync([credit, pix]);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CashSessionPaymentReconciliation>()
                .AsNoTracking()
                .Where(x => x.CashSessionId == 7)
                .ToListAsync();

            persisted.Should().HaveCount(2);
            persisted.Should().Contain(x => x.PaymentMethodId == 2 && x.DifferenceAmount == -10m);
            persisted.Should().Contain(x => x.PaymentMethodId == 4 && x.DifferenceAmount == 0m);
        }
    }
}
