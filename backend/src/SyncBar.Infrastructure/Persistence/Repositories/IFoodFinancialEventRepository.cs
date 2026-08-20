using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodFinancialEventRepository(AppDbContext context) : IIFoodFinancialEventRepository
{
    public async Task<bool> ExistsByIFoodEventIdAsync(long branchId, string ifoodEventId, CancellationToken cancellationToken = default)
        => await context.IFoodFinancialEvents.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.IFoodEventId == ifoodEventId, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodFinancialEvent>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
        => await context.IFoodFinancialEvents.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.CompetenceDate >= periodStart && x.CompetenceDate <= periodEnd)
            .OrderBy(x => x.CompetenceDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodFinancialEvent entity, CancellationToken cancellationToken = default)
        => await context.IFoodFinancialEvents.AddAsync(entity, cancellationToken);
}
