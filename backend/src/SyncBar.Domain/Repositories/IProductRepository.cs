using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    // Usado pela sincronização de pedidos iFood: casa o `ean` do item do pedido com o código de
    // barras do catálogo. Não há sincronização de cardápio ainda — esse é o único vínculo hoje.
    Task<Product?> GetByBarcodeAsync(long companyId, string barcode, CancellationToken cancellationToken = default);
    Task AddAsync(Product entity, CancellationToken cancellationToken = default);
}
