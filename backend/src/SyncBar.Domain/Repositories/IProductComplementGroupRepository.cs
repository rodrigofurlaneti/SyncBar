using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IProductComplementGroupRepository
{
    // Tracked — edição do vínculo (ordem/desativação) precisa da entidade rastreada.
    Task<ProductComplementGroup?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductForUpdateAsync(long productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductAsync(long productId, CancellationToken cancellationToken = default);
    // Usado pela sincronização de catálogo: todos os produtos que usam um dado grupo.
    Task<IReadOnlyCollection<ProductComplementGroup>> GetByComplementGroupAsync(long complementGroupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductsAsync(IReadOnlyCollection<long> productIds, CancellationToken cancellationToken = default);
    Task AddAsync(ProductComplementGroup entity, CancellationToken cancellationToken = default);
}
