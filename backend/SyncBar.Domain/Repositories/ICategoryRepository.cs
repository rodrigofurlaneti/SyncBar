using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(Category entity, CancellationToken cancellationToken = default);
}
