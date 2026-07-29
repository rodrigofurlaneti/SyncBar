using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IProductStockRepository
{
    /// <summary>
    /// Busca o snapshot atual (CurrentBalance e RowVersion) de um produto.
    /// Retorna nulo se o produto não for controlado por estoque.
    /// </summary>
    Task<ProductStock?> GetByProductIdAsync(long productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra uma nova movimentação no livro-razão (Append-Only).
    /// </summary>
    void AddMovement(StockMovement movement);

    /// <summary>
    /// Cria o snapshot inicial de estoque para um produto recém-cadastrado.
    /// </summary>
    void Add(ProductStock stockSnapshot);
}