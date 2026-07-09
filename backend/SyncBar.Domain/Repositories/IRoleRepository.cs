using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Role>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(Role entity, CancellationToken cancellationToken = default);
}
