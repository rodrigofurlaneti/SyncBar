using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPizzaFlavorRepository
{
    Task<PizzaFlavor?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PizzaFlavor?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PizzaFlavor>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PizzaFlavor>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task AddAsync(PizzaFlavor entity, CancellationToken cancellationToken = default);
}
