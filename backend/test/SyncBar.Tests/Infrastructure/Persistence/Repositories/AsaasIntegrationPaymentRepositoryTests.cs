using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AsaasIntegrationPaymentRepositoryTests : RepositoryTestBase
    {
        private readonly AsaasIntegrationPaymentRepository _repository;

        public AsaasIntegrationPaymentRepositoryTests()
        {
            _repository = new AsaasIntegrationPaymentRepository(Context);
        }

        private static AsaasIntegrationPayment CreatePayment(
            long branchId = 1,
            long customerOrderId = 1,
            string asaasPaymentId = "pay_1",
            decimal value = 100m) =>
            AsaasIntegrationPayment.Create(
                branchId, customerOrderId, customerId: null, asaasPaymentId, "PIX", value, DateTime.UtcNow.AddDays(1)).Value;

        private async Task<AsaasIntegrationPayment> SeedAsync(AsaasIntegrationPayment payment)
        {
            await Context.AddAsync(payment);
            await Context.SaveChangesAsync();
            Context.Entry(payment).State = EntityState.Detached;
            return payment;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingActiveId_ReturnsUntrackedPayment()
        {
            var payment = await SeedAsync(CreatePayment());

            var result = await _repository.GetByIdAsync(payment.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(payment.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_DeactivatedPayment_ReturnsNull()
        {
            var payment = await SeedAsync(CreatePayment());
            var tracked = await _repository.GetByIdForUpdateAsync(payment.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(payment.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByAsaasPaymentIdAsync_ExistingId_ReturnsPayment()
        {
            var payment = await SeedAsync(CreatePayment(asaasPaymentId: "pay_abc"));

            var result = await _repository.GetByAsaasPaymentIdAsync("pay_abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(payment.Id);
        }

        [Fact]
        public async Task GetByAsaasPaymentIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByAsaasPaymentIdAsync("pay_missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCustomerOrderIdAsync_ExistingOrder_ReturnsPayment()
        {
            var payment = await SeedAsync(CreatePayment(customerOrderId: 42));

            var result = await _repository.GetByCustomerOrderIdAsync(42);

            result.Should().NotBeNull();
            result!.Id.Should().Be(payment.Id);
        }

        [Fact]
        public async Task GetByCustomerOrderIdAsync_NoPaymentForOrder_ReturnsNull()
        {
            var result = await _repository.GetByCustomerOrderIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchIdAsync_MultiplePayments_ReturnsOrderedByCreatedAtDescending()
        {
            var older = await SeedAsync(CreatePayment(branchId: 5, customerOrderId: 1, asaasPaymentId: "pay_old"));
            await Task.Delay(10);
            var newer = await SeedAsync(CreatePayment(branchId: 5, customerOrderId: 2, asaasPaymentId: "pay_new"));

            var result = await _repository.GetByBranchIdAsync(5);

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(newer.Id);
            result[1].Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetByBranchIdAsync_NoPaymentsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPendingByBranchIdAsync_MixOfStatuses_ReturnsOnlyPending()
        {
            var pending = await SeedAsync(CreatePayment(branchId: 5, customerOrderId: 1, asaasPaymentId: "pay_pending"));
            var confirmed = await SeedAsync(CreatePayment(branchId: 5, customerOrderId: 2, asaasPaymentId: "pay_confirmed"));
            var trackedConfirmed = await _repository.GetByIdForUpdateAsync(confirmed.Id);
            trackedConfirmed!.UpdateStatus("CONFIRMED");
            await Context.SaveChangesAsync();

            var result = await _repository.GetPendingByBranchIdAsync(5);

            result.Should().ContainSingle(x => x.Id == pending.Id);
        }

        [Fact]
        public async Task GetPendingByBranchIdAsync_NoPendingPayments_ReturnsEmptyList()
        {
            var result = await _repository.GetPendingByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByAsaasPaymentIdAsync_ExistingActivePayment_ReturnsTrue()
        {
            await SeedAsync(CreatePayment(asaasPaymentId: "pay_exists"));

            var result = await _repository.ExistsByAsaasPaymentIdAsync("pay_exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByAsaasPaymentIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByAsaasPaymentIdAsync("pay_missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedPayment()
        {
            var payment = await SeedAsync(CreatePayment());

            var result = await _repository.GetByIdForUpdateAsync(payment.Id);

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
        public async Task GetByAsaasPaymentIdForUpdateAsync_ExistingId_ReturnsTrackedPayment()
        {
            var payment = await SeedAsync(CreatePayment(asaasPaymentId: "pay_track"));

            var result = await _repository.GetByAsaasPaymentIdForUpdateAsync("pay_track");

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByAsaasPaymentIdForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByAsaasPaymentIdForUpdateAsync("pay_missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidPayment_PersistsToDatabase()
        {
            var payment = CreatePayment(asaasPaymentId: "pay_new_add");

            await _repository.AddAsync(payment);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AsaasPaymentId == "pay_new_add");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingPayment_PersistsChanges()
        {
            var payment = await SeedAsync(CreatePayment());
            var tracked = await _repository.GetByIdForUpdateAsync(payment.Id);
            tracked!.MarkAsPaid(netValue: 95m);

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == payment.Id);

            persisted.Status.Should().Be("RECEIVED");
            persisted.NetValue.Should().Be(95m);
        }

        [Fact]
        public async Task Delete_ExistingPayment_RemovesFromDatabase()
        {
            var payment = await SeedAsync(CreatePayment());
            var tracked = await _repository.GetByIdForUpdateAsync(payment.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == payment.Id);

            persisted.Should().BeNull();
        }
    }
}
