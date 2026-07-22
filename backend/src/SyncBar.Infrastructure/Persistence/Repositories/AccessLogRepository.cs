using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class AccessLogRepository(AppDbContext context) : IAccessLogRepository
{
    public async Task AddAsync(AccessLog entity, CancellationToken cancellationToken = default)
        => await context.AccessLogs.AddAsync(entity, cancellationToken);
}
