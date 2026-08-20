using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodSettlementRepository(AppDbContext context) : IIFoodSettlementRepository
{
    public async Task<IFoodSettlement?> GetByIFoodSettlementIdForUpdateAsync(long branchId, string ifoodSettlementId, CancellationToken cancellationToken = default)
        => await context.IFoodSettlements
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IFoodSettlementId == ifoodSettlementId, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodSettlement>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
        => await context.IFoodSettlements.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive &&
                        ((x.PaymentDate ?? x.CreatedAt) >= periodStart && (x.PaymentDate ?? x.CreatedAt) <= periodEnd))
            .OrderBy(x => x.PaymentDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodSettlement entity, CancellationToken cancellationToken = default)
        => await context.IFoodSettlements.AddAsync(entity, cancellationToken);
}
