using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodOpeningHoursRepository(AppDbContext context) : IIFoodOpeningHoursRepository
{
    public async Task<IReadOnlyCollection<IFoodOpeningHours>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodOpeningHours.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.Start)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<IFoodOpeningHours>> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodOpeningHours
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<IFoodOpeningHours> entities, CancellationToken cancellationToken = default)
        => await context.IFoodOpeningHours.AddRangeAsync(entities, cancellationToken);
}
