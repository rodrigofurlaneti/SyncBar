using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class LogTrackerRepository(AppDbContext context) : ILogTrackerRepository
    {
        public async Task AddAsync(LogTracker entity, CancellationToken cancellationToken = default)
            => await context.LogTrackers.AddAsync(entity, cancellationToken);
    }
}
