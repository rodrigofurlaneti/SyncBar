using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfMeasureRepository(AppDbContext context) : IUnitOfMeasureRepository
{
    public async Task<UnitOfMeasure?> GetFirstActiveAsync(CancellationToken cancellationToken = default)
        => await context.UnitOfMeasures.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
