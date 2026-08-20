using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodComplementGroupMappingRepository(AppDbContext context) : IIFoodComplementGroupMappingRepository
{
    public async Task<IFoodComplementGroupMapping?> GetByComplementGroupAndBranchAsync(long complementGroupId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodComplementGroupMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ComplementGroupId == complementGroupId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodComplementGroupMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodComplementGroupMappings.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodComplementGroupMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodComplementGroupMappings.AddAsync(entity, cancellationToken);
}
