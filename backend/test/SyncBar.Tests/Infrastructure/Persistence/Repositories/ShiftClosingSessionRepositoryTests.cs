using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ShiftClosingSessionRepositoryTests : RepositoryTestBase
    {
        private readonly ShiftClosingSessionRepository _repository;

        public ShiftClosingSessionRepositoryTests()
        {
            _repository = new ShiftClosingSessionRepository(Context);
        }

        private static ShiftClosingSession CreateSession(long shiftClosingId = 1, long cashSessionId = 1) =>
            ShiftClosingSession.Create(shiftClosingId, cashSessionId).Value;

        private async Task<ShiftClosingSession> SeedAsync(ShiftClosingSession session)
        {
            await Context.AddAsync(session);
            await Context.SaveChangesAsync();
            Context.Entry(session).State = EntityState.Detached;
            return session;
        }

        [Fact]
        public async Task GetByShiftClosingAsync_MultipleActiveSessions_ReturnsAllForShiftClosing()
        {
            var first = await SeedAsync(CreateSession(shiftClosingId: 5, cashSessionId: 1));
            var second = await SeedAsync(CreateSession(shiftClosingId: 5, cashSessionId: 2));
            await SeedAsync(CreateSession(shiftClosingId: 6, cashSessionId: 3));

            var result = await _repository.GetByShiftClosingAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByShiftClosingAsync_InactiveSession_IsExcluded()
        {
            var session = await SeedAsync(CreateSession(shiftClosingId: 5));
            session.Deactivate();
            Context.Update(session);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByShiftClosingAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByShiftClosingAsync_NoSessionsForShiftClosing_ReturnsEmptyList()
        {
            var result = await _repository.GetByShiftClosingAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddRangeAsync_ValidSessions_PersistsToDatabase()
        {
            var first = CreateSession(shiftClosingId: 7, cashSessionId: 1);
            var second = CreateSession(shiftClosingId: 7, cashSessionId: 2);

            await _repository.AddRangeAsync([first, second]);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ShiftClosingSession>()
                .AsNoTracking()
                .Where(x => x.ShiftClosingId == 7)
                .ToListAsync();

            persisted.Should().HaveCount(2);
        }
    }
}
