using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationOrderEventLogRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationOrderEventLogRepository _repository;

        public KeetaIntegrationOrderEventLogRepositoryTests()
        {
            _repository = new KeetaIntegrationOrderEventLogRepository(Context);
        }

        private static KeetaIntegrationOrderEventLog CreateEventLog(
            long companyId = 1, long branchId = 1, string eventId = "evt-1",
            string orderId = "korder-1", string eventType = "NEW") =>
            KeetaIntegrationOrderEventLog.Create(companyId, branchId, eventId, orderId, eventType, "{}", DateTime.UtcNow).Value;

        private async Task<KeetaIntegrationOrderEventLog> SeedAsync(KeetaIntegrationOrderEventLog log)
        {
            await Context.AddAsync(log);
            await Context.SaveChangesAsync();
            Context.Entry(log).State = EntityState.Detached;
            return log;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedLog()
        {
            var log = await SeedAsync(CreateEventLog());

            var result = await _repository.GetByIdAsync(log.Id);

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
        public async Task GetByEventIdAsync_ExistingId_ReturnsTrackedLog()
        {
            var log = await SeedAsync(CreateEventLog(eventId: "evt-abc"));

            var result = await _repository.GetByEventIdAsync("evt-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(log.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByEventIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByEventIdAsync("evt-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByOrderIdAsync_MultipleEvents_ReturnsAllForOrder()
        {
            var first = await SeedAsync(CreateEventLog(eventId: "evt-1", orderId: "korder-x"));
            var second = await SeedAsync(CreateEventLog(eventId: "evt-2", orderId: "korder-x"));
            await SeedAsync(CreateEventLog(eventId: "evt-3", orderId: "korder-y"));

            var result = await _repository.GetAllByOrderIdAsync("korder-x");

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetAllByOrderIdAsync_NoEventsForOrder_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByOrderIdAsync("korder-missing");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnprocessedEventsAsync_MixOfProcessedAndPending_ReturnsOnlyUnprocessed()
        {
            var pending = await SeedAsync(CreateEventLog(eventId: "evt-pending"));
            var processed = await SeedAsync(CreateEventLog(eventId: "evt-processed"));
            var tracked = await _repository.GetByEventIdAsync("evt-processed");
            tracked!.MarkAsProcessed();
            await Context.SaveChangesAsync();

            var result = await _repository.GetUnprocessedEventsAsync();

            result.Should().ContainSingle(x => x.Id == pending.Id);
            result.Should().NotContain(x => x.Id == processed.Id);
        }

        [Fact]
        public async Task GetUnprocessedEventsAsync_NoEvents_ReturnsEmptyList()
        {
            var result = await _repository.GetUnprocessedEventsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByEventIdAsync_Existing_ReturnsTrue()
        {
            await SeedAsync(CreateEventLog(eventId: "evt-exists"));

            var result = await _repository.ExistsByEventIdAsync("evt-exists");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEventIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByEventIdAsync("evt-missing");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidLog_PersistsToDatabase()
        {
            var log = CreateEventLog(eventId: "evt-added");

            await _repository.AddAsync(log);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrderEventLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventId == "evt-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingLog_PersistsChanges()
        {
            var log = await SeedAsync(CreateEventLog());
            var tracked = await _repository.GetByEventIdAsync(log.EventId);
            tracked!.MarkAsFailed("erro-teste");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrderEventLog>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == log.Id);

            persisted.ProcessedSuccessfully.Should().BeFalse();
            persisted.ErrorMessage.Should().Be("erro-teste");
        }

        [Fact]
        public async Task Delete_ExistingLog_RemovesFromDatabase()
        {
            var log = await SeedAsync(CreateEventLog());
            var tracked = await _repository.GetByEventIdAsync(log.EventId);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationOrderEventLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == log.Id);

            persisted.Should().BeNull();
        }
    }
}
