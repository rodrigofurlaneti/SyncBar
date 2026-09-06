using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class WaiterMessageRepositoryTests : RepositoryTestBase
    {
        private readonly WaiterMessageRepository _repository;

        public WaiterMessageRepositoryTests()
        {
            _repository = new WaiterMessageRepository(Context);
        }

        private static WaiterMessage CreateMessage(long branchId = 1, string message = "Mesa 5 precisa de atenção") =>
            WaiterMessage.Create(branchId, senderEmployeeId: 1, recipientEmployeeId: null, diningAreaId: 1, message: message).Value;

        private async Task<WaiterMessage> SeedAsync(WaiterMessage message)
        {
            await Context.AddAsync(message);
            await Context.SaveChangesAsync();
            Context.Entry(message).State = EntityState.Detached;
            return message;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsTrackedMessage()
        {
            var message = await SeedAsync(CreateMessage());

            var result = await _repository.GetByIdAsync(message.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(message.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchIdAsync_MultipleActiveMessages_ReturnsOrderedByCreatedAt()
        {
            var first = await SeedAsync(CreateMessage(branchId: 5, message: "Primeira"));
            await Task.Delay(10);
            var second = await SeedAsync(CreateMessage(branchId: 5, message: "Segunda"));
            await SeedAsync(CreateMessage(branchId: 6, message: "Outra filial"));

            var result = (await _repository.GetByBranchIdAsync(5)).ToList();

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(first.Id);
            result[1].Id.Should().Be(second.Id);
        }

        [Fact]
        public async Task GetByBranchIdAsync_InactiveMessage_IsExcluded()
        {
            var message = await SeedAsync(CreateMessage(branchId: 5));
            var tracked = await Context.Set<WaiterMessage>().FirstAsync(x => x.Id == message.Id);
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchIdAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchIdAsync_NoMessagesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMessage_PersistsToDatabase()
        {
            var message = CreateMessage(branchId: 7, message: "Mensagem nova");

            await _repository.AddAsync(message);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<WaiterMessage>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Message == "Mensagem nova");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingMessage_PersistsChanges()
        {
            var message = await SeedAsync(CreateMessage());
            var tracked = await Context.Set<WaiterMessage>().FirstAsync(x => x.Id == message.Id);
            tracked.Deactivate();

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<WaiterMessage>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == message.Id);

            persisted.IsActive.Should().BeFalse();
        }
    }
}
