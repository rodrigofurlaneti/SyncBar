using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodFinancialEventRepository(AppDbContext context) : IIfoodFinancialEventRepository
{
    public async Task<bool> ExistsByIfoodEventIdAsync(long branchId, string IfoodEventId, CancellationToken cancellationToken = default)
        => await context.IfoodFinancialEvents.AsNoTracking()
            .AnyAsync(x => x.BranchId == branchId && x.IfoodEventId == IfoodEventId, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodFinancialEvent>> GetByBranchAndPeriodAsync(
        long branchId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
        => await context.IfoodFinancialEvents.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.CompetenceDate >= periodStart && x.CompetenceDate <= periodEnd)
            .OrderBy(x => x.CompetenceDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodFinancialEvent entity, CancellationToken cancellationToken = default)
        => await context.IfoodFinancialEvents.AddAsync(entity, cancellationToken);
}
