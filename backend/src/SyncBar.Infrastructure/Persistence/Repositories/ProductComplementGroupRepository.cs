using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ProductComplementGroupRepository(AppDbContext context) : IProductComplementGroupRepository
{
    // Tracked — edição do vínculo (ordem/desativação) precisa da entidade rastreada.
    public async Task<ProductComplementGroup?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductForUpdateAsync(long productId, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductAsync(long productId, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups.AsNoTracking()
            .Where(x => x.ProductId == productId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductComplementGroup>> GetByComplementGroupAsync(long complementGroupId, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups.AsNoTracking()
            .Where(x => x.ComplementGroupId == complementGroupId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductComplementGroup>> GetByProductsAsync(IReadOnlyCollection<long> productIds, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductComplementGroup entity, CancellationToken cancellationToken = default)
        => await context.ProductComplementGroups.AddAsync(entity, cancellationToken);
}
