using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PromotionRepository(AppDbContext context) : IPromotionRepository
{
    public async Task<Promotion?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Promotions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Promotion>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Promotions.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Promotion entity, CancellationToken cancellationToken = default)
        => await context.Promotions.AddAsync(entity, cancellationToken);
}
