using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface ILogTrackerRepository
    {
        Task AddAsync(LogTracker entity, CancellationToken cancellationToken = default);
    }
}
