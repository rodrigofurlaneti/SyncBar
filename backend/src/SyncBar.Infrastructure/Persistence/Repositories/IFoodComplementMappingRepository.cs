using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodComplementMappingRepository(AppDbContext context) : IIfoodComplementMappingRepository
{
    public async Task<IfoodComplementMapping?> GetByComplementAndBranchAsync(long complementId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodComplementMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ComplementId == complementId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodComplementMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodComplementMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IfoodComplementMapping?> GetByIfoodOptionIdAndBranchAsync(Guid IfoodOptionId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodComplementMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IfoodOptionId == IfoodOptionId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(IfoodComplementMapping entity, CancellationToken cancellationToken = default)
        => await context.IfoodComplementMappings.AddAsync(entity, cancellationToken);
}
