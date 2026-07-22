using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IAppFeatureRepository
{
    Task<IReadOnlyCollection<AppFeature>> GetAllAsync(CancellationToken cancellationToken = default);
}
