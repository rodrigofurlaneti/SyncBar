using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual a GetByCompanyAsync, mas SEM o filtro IsActive — inclui produtos desativados.
    /// Uso restrito à tela de gerenciamento (Cardápio admin); telas de pedido/venda devem
    /// continuar usando GetByCompanyAsync para nunca oferecer um produto desativado.
    /// </summary>
    Task<IReadOnlyCollection<Product>> GetAllByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(long companyId, string barcode, CancellationToken cancellationToken = default);
    Task AddAsync(Product entity, CancellationToken cancellationToken = default);
}
