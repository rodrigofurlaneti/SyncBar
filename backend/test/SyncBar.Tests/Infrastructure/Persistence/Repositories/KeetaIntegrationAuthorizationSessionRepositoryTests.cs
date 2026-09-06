using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationAuthorizationSessionRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationAuthorizationSessionRepository _repository;

        public KeetaIntegrationAuthorizationSessionRepositoryTests()
        {
            _repository = new KeetaIntegrationAuthorizationSessionRepository(Context);
        }

        private static KeetaIntegrationAuthorizationSession CreateSession(
            long companyId = 1, long branchId = 1, string authId = "auth-1", int operationType = 1) =>
            KeetaIntegrationAuthorizationSession.Create(companyId, branchId, authId, operationType).Value;

        private async Task<KeetaIntegrationAuthorizationSession> SeedAsync(KeetaIntegrationAuthorizationSession session)
        {
            await Context.AddAsync(session);
            await Context.SaveChangesAsync();
            Context.Entry(session).State = EntityState.Detached;
            return session;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedSession()
        {
            var session = await SeedAsync(CreateSession());

            var result = await _repository.GetByIdAsync(session.Id);

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
        public async Task GetByAuthIdAsync_ExistingAuthId_ReturnsTrackedSession()
        {
            var session = await SeedAsync(CreateSession(authId: "auth-abc"));

            var result = await _repository.GetByAuthIdAsync("auth-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(session.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByAuthIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByAuthIdAsync("auth-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPendingSessionsAsync_MixOfProcessedAndPending_ReturnsOnlyPending()
        {
            var pending = await SeedAsync(CreateSession(authId: "auth-pending"));
            var processed = await SeedAsync(CreateSession(authId: "auth-processed"));
            var tracked = await _repository.GetByAuthIdAsync("auth-processed");
            tracked!.MarkAsProcessed();
            await Context.SaveChangesAsync();

            var result = await _repository.GetPendingSessionsAsync();

            result.Should().ContainSingle(x => x.Id == pending.Id);
            result.Should().NotContain(x => x.Id == processed.Id);
        }

        [Fact]
        public async Task GetPendingSessionsAsync_NoSessions_ReturnsEmptyList()
        {
            var result = await _repository.GetPendingSessionsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidSession_PersistsToDatabase()
        {
            var session = CreateSession(authId: "auth-added");

            await _repository.AddAsync(session);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationAuthorizationSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AuthId == "auth-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingSession_PersistsChanges()
        {
            var session = await SeedAsync(CreateSession());
            var tracked = await _repository.GetByAuthIdAsync(session.AuthId);
            tracked!.SetDetails(keetaMerchantId: 42, authorizationCode: "code-1", state: "state-1");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationAuthorizationSession>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == session.Id);

            persisted.KeetaMerchantId.Should().Be(42);
            persisted.AuthorizationCode.Should().Be("code-1");
        }

        [Fact]
        public async Task Delete_ExistingSession_RemovesFromDatabase()
        {
            var session = await SeedAsync(CreateSession());
            var tracked = await _repository.GetByAuthIdAsync(session.AuthId);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationAuthorizationSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == session.Id);

            persisted.Should().BeNull();
        }
    }
}
