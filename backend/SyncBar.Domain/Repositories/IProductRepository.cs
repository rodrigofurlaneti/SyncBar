using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(Product entity, CancellationToken cancellationToken = default);
}
