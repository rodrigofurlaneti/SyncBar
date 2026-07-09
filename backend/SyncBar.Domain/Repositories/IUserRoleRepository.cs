using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IUserRoleRepository
{
    // Tracked — atribuicao de perfis desativa/reativa vinculos.
    Task<IReadOnlyCollection<UserRole>> GetByUserForUpdateAsync(long appUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserRole>> GetByUsersAsync(IReadOnlyCollection<long> appUserIds, CancellationToken cancellationToken = default);
    Task AddAsync(UserRole entity, CancellationToken cancellationToken = default);
}
