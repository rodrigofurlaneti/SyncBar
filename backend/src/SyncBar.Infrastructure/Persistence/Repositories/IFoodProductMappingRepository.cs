using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodProductMappingRepository(AppDbContext context) : IIfoodProductMappingRepository
{
    public async Task<IfoodProductMapping?> GetByProductAndBranchAsync(long productId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodProductMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodProductMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodProductMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodProductMapping entity, CancellationToken cancellationToken = default)
        => await context.IfoodProductMappings.AddAsync(entity, cancellationToken);
}
