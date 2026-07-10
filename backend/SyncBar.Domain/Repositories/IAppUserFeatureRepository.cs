using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IAppUserFeatureRepository
{
    Task<IReadOnlyCollection<AppUserFeature>> GetByUserAsync(long appUserId, CancellationToken cancellationToken = default);
    // Tracked — concessao desativa/reativa vinculos.
    Task<IReadOnlyCollection<AppUserFeature>> GetByUserForUpdateAsync(long appUserId, CancellationToken cancellationToken = default);
    Task AddAsync(AppUserFeature entity, CancellationToken cancellationToken = default);
}
