using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodOpeningHoursRepository(AppDbContext context) : IIfoodOpeningHoursRepository
{
    public async Task<IReadOnlyCollection<IfoodOpeningHours>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodOpeningHours.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.Start)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<IfoodOpeningHours>> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodOpeningHours
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<IfoodOpeningHours> entities, CancellationToken cancellationToken = default)
        => await context.IfoodOpeningHours.AddRangeAsync(entities, cancellationToken);
}
