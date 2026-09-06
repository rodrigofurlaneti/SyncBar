using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Enums;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AsaasIntegrationWebhookLogRepositoryTests : RepositoryTestBase
    {
        private readonly AsaasIntegrationWebhookLogRepository _repository;

        public AsaasIntegrationWebhookLogRepositoryTests()
        {
            _repository = new AsaasIntegrationWebhookLogRepository(Context);
        }

        private static AsaasIntegrationWebhookLog CreateLog(
            long companyId = 1,
            long? branchId = null,
            string @event = "PAYMENT_CREATED",
            string? asaasEventId = "evt_1",
            string? paymentId = "pay_1") =>
            AsaasIntegrationWebhookLog.Create(companyId, branchId, @event, asaasEventId, paymentId, "{}").Value;

        private async Task<AsaasIntegrationWebhookLog> SeedAsync(AsaasIntegrationWebhookLog log)
        {
            await Context.AddAsync(log);
            await Context.SaveChangesAsync();
            Context.Entry(log).State = EntityState.Detached;
            return log;
        }

        [Fact]
        public async Task GetAsync_MultipleActiveLogs_ReturnsAll()
        {
            var first = await SeedAsync(CreateLog(asaasEventId: "evt_1"));
            var second = await SeedAsync(CreateLog(asaasEventId: "evt_2"));

            var result = (await _repository.GetAsync()).ToList();

            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetAsync_NoLogs_ReturnsEmpty()
        {
            var result = await _repository.GetAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingActiveLog_ReturnsUntrackedLog()
        {
            var log = await SeedAsync(CreateLog());

            var result = await _repository.GetByIdAsync(log.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_DeactivatedLog_ReturnsNull()
        {
            var log = await SeedAsync(CreateLog());
            var tracked = await _repository.GetByIdForUpdateAsync(log.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(log.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByPaymentIdAsync_MultipleLogsSamePayment_ReturnsOrderedByCreatedAtDescending()
        {
            var older = await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_old", paymentId: "pay_x"));
            await Task.Delay(10);
            var newer = await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_new", paymentId: "pay_x"));
            await SeedAsync(CreateLog(companyId: 6, asaasEventId: "evt_other_company", paymentId: "pay_x"));

            var result = await _repository.GetByPaymentIdAsync(5, "pay_x");

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(newer.Id);
            result[1].Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetByPaymentIdAsync_NoLogsForPayment_ReturnsEmptyList()
        {
            var result = await _repository.GetByPaymentIdAsync(999, "pay_missing");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnprocessedLogsAsync_MixOfStatuses_ReturnsOnlyPendingOrderedAscending()
        {
            var older = await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_old"));
            await Task.Delay(10);
            var newer = await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_new"));
            var processed = await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_processed"));
            var trackedProcessed = await _repository.GetByIdForUpdateAsync(processed.Id);
            trackedProcessed!.MarkAsProcessed();
            await Context.SaveChangesAsync();

            var result = await _repository.GetUnprocessedLogsAsync(5);

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(older.Id);
            result[1].Id.Should().Be(newer.Id);
        }

        [Fact]
        public async Task GetUnprocessedLogsAsync_LimitLowerThanAvailable_RespectsLimit()
        {
            await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_1"));
            await SeedAsync(CreateLog(companyId: 5, asaasEventId: "evt_2"));

            var result = await _repository.GetUnprocessedLogsAsync(5, limit: 1);

            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUnprocessedLogsAsync_NoPendingLogs_ReturnsEmptyList()
        {
            var result = await _repository.GetUnprocessedLogsAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByEventIdAsync_ExistingActiveEvent_ReturnsTrue()
        {
            await SeedAsync(CreateLog(asaasEventId: "evt_exists"));

            var result = await _repository.ExistsByEventIdAsync("evt_exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEventIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByEventIdAsync("evt_missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAlreadyProcessedEventAsync_ProcessedEvent_ReturnsTrue()
        {
            var log = await SeedAsync(CreateLog(asaasEventId: "evt_done"));
            var tracked = await _repository.GetByIdForUpdateAsync(log.Id);
            tracked!.MarkAsProcessed();
            await Context.SaveChangesAsync();

            var result = await _repository.HasAlreadyProcessedEventAsync("evt_done");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasAlreadyProcessedEventAsync_PendingEvent_ReturnsFalse()
        {
            await SeedAsync(CreateLog(asaasEventId: "evt_pending"));

            var result = await _repository.HasAlreadyProcessedEventAsync("evt_pending");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAlreadyProcessedEventAsync_NonExistingEvent_ReturnsFalse()
        {
            var result = await _repository.HasAlreadyProcessedEventAsync("evt_missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedLog()
        {
            var log = await SeedAsync(CreateLog());

            var result = await _repository.GetByIdForUpdateAsync(log.Id);

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
        public async Task AddAsync_ValidLog_PersistsToDatabase()
        {
            var log = CreateLog(asaasEventId: "evt_added");

            await _repository.AddAsync(log);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationWebhookLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AsaasEventId == "evt_added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.Status.Should().Be(WebhookLogStatus.Pending);
        }

        [Fact]
        public async Task Update_ExistingLog_PersistsChanges()
        {
            var log = await SeedAsync(CreateLog());
            var tracked = await _repository.GetByIdForUpdateAsync(log.Id);
            tracked!.MarkAsFailed("timeout");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationWebhookLog>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == log.Id);

            persisted.Status.Should().Be(WebhookLogStatus.Failed);
            persisted.ErrorMessage.Should().Be("timeout");
        }

        [Fact]
        public async Task Delete_ExistingLog_RemovesFromDatabase()
        {
            var log = await SeedAsync(CreateLog());
            var tracked = await _repository.GetByIdForUpdateAsync(log.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationWebhookLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == log.Id);

            persisted.Should().BeNull();
        }
    }
}
