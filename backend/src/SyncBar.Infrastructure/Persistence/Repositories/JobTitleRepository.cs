using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class JobTitleRepository(AppDbContext context) : IJobTitleRepository
{
    public async Task<JobTitle?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.JobTitles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<JobTitle>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.JobTitles.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(JobTitle entity, CancellationToken cancellationToken = default)
        => await context.JobTitles.AddAsync(entity, cancellationToken);
}
