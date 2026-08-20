using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodProductMappingRepository(AppDbContext context) : IIFoodProductMappingRepository
{
    public async Task<IFoodProductMapping?> GetByProductAndBranchAsync(long productId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodProductMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodProductMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodProductMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodProductMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodProductMappings.AddAsync(entity, cancellationToken);
}
