using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class LogTrackerRepositoryTests : RepositoryTestBase
    {
        private readonly LogTrackerRepository _repository;

        public LogTrackerRepositoryTests()
        {
            _repository = new LogTrackerRepository(Context);
        }

        [Fact]
        public async Task AddAsync_ValidLogTracker_PersistsToDatabase()
        {
            var log = new LogTracker(0)
            {
                DirectoryName = "Application/Features",
                ClassName = "TestClassName",
                MethodName = "TestMethodName",
                IsSuccess = true,
                ExecutionTimeMs = 10,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _repository.AddAsync(log);
            await Context.SaveChangesAsync();

            var persisted = await Context.LogTrackers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClassName == "TestClassName");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.MethodName.Should().Be("TestMethodName");
        }
    }
}
