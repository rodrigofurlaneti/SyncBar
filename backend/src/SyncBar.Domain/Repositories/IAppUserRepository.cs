using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AppUser>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    // Tracked — Login atualiza FailedAccessCount/LastLoginAt.
    Task<AppUser?> GetByUserNameForUpdateAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userName, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(long appUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(long appUserId, CancellationToken cancellationToken = default);
    Task AddAsync(AppUser entity, CancellationToken cancellationToken = default);
}
