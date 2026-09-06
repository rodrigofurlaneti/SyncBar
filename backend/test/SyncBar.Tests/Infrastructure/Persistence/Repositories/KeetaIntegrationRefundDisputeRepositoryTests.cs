using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationRefundDisputeRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationRefundDisputeRepository _repository;

        public KeetaIntegrationRefundDisputeRepositoryTests()
        {
            _repository = new KeetaIntegrationRefundDisputeRepository(Context);
        }

        private static KeetaIntegrationRefundDispute CreateDispute(
            long companyId = 1, long branchId = 1, string orderId = "korder-1",
            long afterSaleOrderId = 1000, decimal refundAmount = 20m) =>
            KeetaIntegrationRefundDispute.Create(companyId, branchId, orderId, afterSaleOrderId, refundAmount, "cliente pediu").Value;

        private async Task<KeetaIntegrationRefundDispute> SeedAsync(KeetaIntegrationRefundDispute dispute)
        {
            await Context.AddAsync(dispute);
            await Context.SaveChangesAsync();
            Context.Entry(dispute).State = EntityState.Detached;
            return dispute;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedDispute()
        {
            var dispute = await SeedAsync(CreateDispute());

            var result = await _repository.GetByIdAsync(dispute.Id);

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
        public async Task GetByAfterSaleOrderIdAsync_ExistingId_ReturnsTrackedDispute()
        {
            var dispute = await SeedAsync(CreateDispute(afterSaleOrderId: 12345));

            var result = await _repository.GetByAfterSaleOrderIdAsync(12345);

            result.Should().NotBeNull();
            result!.Id.Should().Be(dispute.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByAfterSaleOrderIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByAfterSaleOrderIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByOrderIdAsync_ExistingId_ReturnsTrackedDispute()
        {
            var dispute = await SeedAsync(CreateDispute(orderId: "korder-abc"));

            var result = await _repository.GetByOrderIdAsync("korder-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(dispute.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByOrderIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByOrderIdAsync("korder-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPendingDisputesByBranchAsync_MixOfStatuses_ReturnsOnlyPending()
        {
            var pending = await SeedAsync(CreateDispute(branchId: 10, afterSaleOrderId: 1));
            var resolved = await SeedAsync(CreateDispute(branchId: 10, afterSaleOrderId: 2));
            var tracked = await _repository.GetByAfterSaleOrderIdAsync(2);
            tracked!.Resolve(accepted: true);
            await Context.SaveChangesAsync();

            var result = await _repository.GetPendingDisputesByBranchAsync(10);

            result.Should().ContainSingle(x => x.Id == pending.Id);
            result.Should().NotContain(x => x.Id == resolved.Id);
        }

        [Fact]
        public async Task GetPendingDisputesByBranchAsync_NoPendingDisputes_ReturnsEmptyList()
        {
            var result = await _repository.GetPendingDisputesByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByAfterSaleOrderIdAsync_Existing_ReturnsTrue()
        {
            await SeedAsync(CreateDispute(afterSaleOrderId: 777));

            var result = await _repository.ExistsByAfterSaleOrderIdAsync(777);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByAfterSaleOrderIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByAfterSaleOrderIdAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidDispute_PersistsToDatabase()
        {
            var dispute = CreateDispute(afterSaleOrderId: 888);

            await _repository.AddAsync(dispute);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationRefundDispute>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AfterSaleOrderId == 888);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.ResolutionStatus.Should().Be("PENDING");
        }

        [Fact]
        public async Task Update_ExistingDispute_PersistsChanges()
        {
            var dispute = await SeedAsync(CreateDispute());
            var tracked = await _repository.GetByAfterSaleOrderIdAsync(dispute.AfterSaleOrderId);
            tracked!.Resolve(accepted: false, denialReasonCode: "OUT_OF_POLICY", denialReasonText: "fora do prazo");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationRefundDispute>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == dispute.Id);

            persisted.ResolutionStatus.Should().Be("REJECTED");
            persisted.DenialReasonCode.Should().Be("OUT_OF_POLICY");
        }

        [Fact]
        public async Task Delete_ExistingDispute_RemovesFromDatabase()
        {
            var dispute = await SeedAsync(CreateDispute());
            var tracked = await _repository.GetByAfterSaleOrderIdAsync(dispute.AfterSaleOrderId);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationRefundDispute>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dispute.Id);

            persisted.Should().BeNull();
        }
    }
}
