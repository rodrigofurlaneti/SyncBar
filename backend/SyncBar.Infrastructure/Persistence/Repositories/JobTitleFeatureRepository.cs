using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class JobTitleFeatureRepository(AppDbContext context) : IJobTitleFeatureRepository
{
    public async Task<IReadOnlyCollection<JobTitleFeature>> GetByJobTitleAsync(long jobTitleId, CancellationToken cancellationToken = default)
        => await context.JobTitleFeatures.AsNoTracking()
            .Where(x => x.JobTitleId == jobTitleId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<JobTitleFeature>> GetByJobTitleForUpdateAsync(long jobTitleId, CancellationToken cancellationToken = default)
        => await context.JobTitleFeatures
            .Where(x => x.JobTitleId == jobTitleId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobTitleFeature entity, CancellationToken cancellationToken = default)
        => await context.JobTitleFeatures.AddAsync(entity, cancellationToken);
}
