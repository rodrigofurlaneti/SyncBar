using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class AppFeatureRepository(AppDbContext context) : IAppFeatureRepository
{
    public async Task<IReadOnlyCollection<AppFeature>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.AppFeatures.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
}
