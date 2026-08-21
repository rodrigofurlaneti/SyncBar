using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodIntegrationSettingRepository(AppDbContext context) : IIFoodIntegrationSettingRepository
{
    public async Task<IFoodIntegrationSetting?> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.IFoodIntegrationSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<IFoodIntegrationSetting?> GetByCompanyForUpdateAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.IFoodIntegrationSettings
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<long>> GetEnabledCompanyIdsAsync(CancellationToken cancellationToken = default)
        => await context.IFoodIntegrationSettings.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.IsActive && x.Enabled)
            .Select(x => x.CompanyId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodIntegrationSetting entity, CancellationToken cancellationToken = default)
        => await context.IFoodIntegrationSettings.AddAsync(entity, cancellationToken);
}
