using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class OperatingCostRepository(AppDbContext context) : IOperatingCostRepository
{
    public async Task<OperatingCost?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.OperatingCosts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<OperatingCost>> GetByBranchAndMonthAsync(
        long branchId, int referenceYear, int referenceMonth, CancellationToken cancellationToken = default)
        => await context.OperatingCosts.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.ReferenceYear == referenceYear
                && x.ReferenceMonth == referenceMonth && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OperatingCost entity, CancellationToken cancellationToken = default)
        => await context.OperatingCosts.AddAsync(entity, cancellationToken);
}
