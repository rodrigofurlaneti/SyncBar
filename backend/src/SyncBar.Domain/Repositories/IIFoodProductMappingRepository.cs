using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodProductMappingRepository
{
    Task<IfoodProductMapping?> GetByProductAndBranchAsync(long productId, long branchId, CancellationToken cancellationToken = default);

    // Todos os mapeamentos já criados numa filial, independente do Product ainda estar ativo —
    // usado pela sincronização pra achar itens cujo Product saiu da lista de ativos (foi
    // desativado) e precisa ser pausado (PATCH /items/status) no Ifood.
    Task<IReadOnlyCollection<IfoodProductMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);

    Task AddAsync(IfoodProductMapping entity, CancellationToken cancellationToken = default);
}
