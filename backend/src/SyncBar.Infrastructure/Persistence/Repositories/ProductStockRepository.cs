using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ProductStockRepository(AppDbContext context) : IProductStockRepository
{
    public async Task<ProductStock?> GetByProductIdAsync(long productId, CancellationToken cancellationToken = default)
        => await context.Set<ProductStock>()
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

    public void AddMovement(StockMovement movement)
    {
        context.Set<StockMovement>().Add(movement);
    }

    public void Add(ProductStock stockSnapshot)
    {
        context.Set<ProductStock>().Add(stockSnapshot);
    }
}