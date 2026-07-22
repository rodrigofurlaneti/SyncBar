using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public sealed record ProductQuantity(long ProductId, decimal Quantity);

public interface IStockMovementRepository
{
    Task<IReadOnlyCollection<StockMovement>> GetByStockItemAsync(long stockItemId, CancellationToken cancellationToken = default);
    // CMV: soma do custo das saidas de venda da filial no periodo.
    Task<decimal> GetSaleCostAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    // Mix de vendas: quantidade vendida por produto no periodo.
    Task<IReadOnlyCollection<ProductQuantity>> GetSaleQuantitiesByProductAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default);
}
