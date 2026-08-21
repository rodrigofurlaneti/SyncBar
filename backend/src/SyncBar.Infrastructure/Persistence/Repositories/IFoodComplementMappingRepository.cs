using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodComplementMappingRepository(AppDbContext context) : IIFoodComplementMappingRepository
{
    public async Task<IFoodComplementMapping?> GetByComplementAndBranchAsync(long complementId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodComplementMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ComplementId == complementId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodComplementMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodComplementMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IFoodComplementMapping?> GetByIFoodOptionIdAndBranchAsync(Guid ifoodOptionId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodComplementMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IFoodOptionId == ifoodOptionId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(IFoodComplementMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodComplementMappings.AddAsync(entity, cancellationToken);
}
