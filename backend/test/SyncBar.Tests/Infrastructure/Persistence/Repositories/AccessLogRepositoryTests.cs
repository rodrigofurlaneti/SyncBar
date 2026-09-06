using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AccessLogRepositoryTests : RepositoryTestBase
    {
        private readonly AccessLogRepository _repository;

        public AccessLogRepositoryTests()
        {
            _repository = new AccessLogRepository(Context);
        }

        [Fact]
        public async Task AddAsync_ValidAccessLog_PersistsToDatabase()
        {
            var log = AccessLog.Create(appUserId: 1, userName: "usuario1", eventType: "LOGIN", ipAddress: "127.0.0.1", userAgent: "test-agent").Value;

            await _repository.AddAsync(log);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AccessLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == "usuario1");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.EventType.Should().Be("LOGIN");
        }
    }
}
