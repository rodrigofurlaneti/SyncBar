using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodOrderRepository
{
    Task<IFoodOrder?> GetByIFoodOrderIdAsync(string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodOrder?> GetByIFoodOrderIdForUpdateAsync(string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    // "Abertos" = ainda não concluídos nem cancelados — para a tela "Pedidos iFood".
    Task<IReadOnlyCollection<IFoodOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodOrder entity, CancellationToken cancellationToken = default);
}
