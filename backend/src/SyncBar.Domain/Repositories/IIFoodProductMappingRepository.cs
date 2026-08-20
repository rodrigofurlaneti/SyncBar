using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodProductMappingRepository
{
    Task<IFoodProductMapping?> GetByProductAndBranchAsync(long productId, long branchId, CancellationToken cancellationToken = default);

    // Todos os mapeamentos já criados numa filial, independente do Product ainda estar ativo —
    // usado pela sincronização pra achar itens cujo Product saiu da lista de ativos (foi
    // desativado) e precisa ser pausado (PATCH /items/status) no iFood.
    Task<IReadOnlyCollection<IFoodProductMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);

    Task AddAsync(IFoodProductMapping entity, CancellationToken cancellationToken = default);
}
