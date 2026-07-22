using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IAccessLogRepository
{
    Task AddAsync(AccessLog entity, CancellationToken cancellationToken = default);
}
