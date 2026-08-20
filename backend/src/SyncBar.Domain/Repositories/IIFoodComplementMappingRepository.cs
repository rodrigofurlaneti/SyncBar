using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodComplementMappingRepository
{
    Task<IFoodComplementMapping?> GetByComplementAndBranchAsync(long complementId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IFoodComplementMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    // Usado pela sincronização de pedidos iFood: casa o `option.id` recebido no pedido com o
    // Complement correspondente, por filial — mesma ideia de GetByBranchAsync do product mapping.
    Task<IFoodComplementMapping?> GetByIFoodOptionIdAndBranchAsync(Guid ifoodOptionId, long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodComplementMapping entity, CancellationToken cancellationToken = default);
}
