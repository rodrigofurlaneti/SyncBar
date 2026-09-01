using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodIntegrationSettingRepository(AppDbContext context) : IIfoodIntegrationSettingRepository
{
    public async Task<IfoodIntegrationSetting?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.IfoodIntegrationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<IfoodIntegrationSetting?> GetByCompanyForUpdateAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.IfoodIntegrationSettings
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<long>> GetEnabledCompanyIdsAsync(CancellationToken cancellationToken = default)
        => await context.IfoodIntegrationSettings.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.IsActive && x.Enabled)
            .Select(x => x.CompanyId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodIntegrationSetting entity, CancellationToken cancellationToken = default)
        => await context.IfoodIntegrationSettings.AddAsync(entity, cancellationToken);
}
