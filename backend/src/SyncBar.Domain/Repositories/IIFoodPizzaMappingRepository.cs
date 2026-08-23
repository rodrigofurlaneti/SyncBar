using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodPizzaMappingRepository
{
    // Com Elements — para leitura/sincronização.
    Task<IFoodPizzaMapping?> GetByPizzaConfigurationAndBranchAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default);
    // Tracked — para SetElement/UpdateIFoodPizzaId.
    Task<IFoodPizzaMapping?> GetByPizzaConfigurationAndBranchForUpdateAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IFoodPizzaMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodPizzaMapping entity, CancellationToken cancellationToken = default);
}
