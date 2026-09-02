using SyncBar.Domain.Entities;
using System.Threading;

namespace SyncBar.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(long companyId, string jobTitleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Role>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(long companyId, string name, CancellationToken cancellationToken = default);
    Task AddAsync(Role entity, CancellationToken cancellationToken = default);
}
