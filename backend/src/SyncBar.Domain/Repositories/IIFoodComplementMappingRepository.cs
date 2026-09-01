using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodComplementMappingRepository
{
    Task<IfoodComplementMapping?> GetByComplementAndBranchAsync(long complementId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IfoodComplementMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    // Usado pela sincronização de pedidos Ifood: casa o `option.id` recebido no pedido com o
    // Complement correspondente, por filial — mesma ideia de GetByBranchAsync do product mapping.
    Task<IfoodComplementMapping?> GetByIfoodOptionIdAndBranchAsync(Guid IfoodOptionId, long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodComplementMapping entity, CancellationToken cancellationToken = default);
}
