using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodOrderRepository
{
    Task<IfoodOrder?> GetByIfoodOrderIdAsync(string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodOrder?> GetByIfoodOrderIdForUpdateAsync(string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    // "Abertos" = ainda não concluídos nem cancelados — para a tela "Pedidos Ifood".
    Task<IReadOnlyCollection<IfoodOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodOrder entity, CancellationToken cancellationToken = default);
}
