using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IJobTitleRepository
{
    Task<JobTitle?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JobTitle>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(JobTitle entity, CancellationToken cancellationToken = default);
}
