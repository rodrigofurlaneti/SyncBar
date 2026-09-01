using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodPizzaMappingRepository
{
    // Com Elements — para leitura/sincronização.
    Task<IfoodPizzaMapping?> GetByPizzaConfigurationAndBranchAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default);
    // Tracked — para SetElement/UpdateIfoodPizzaId.
    Task<IfoodPizzaMapping?> GetByPizzaConfigurationAndBranchForUpdateAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IfoodPizzaMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodPizzaMapping entity, CancellationToken cancellationToken = default);
}
