using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class AsaasIntegrationSettingRepository(AppDbContext context) : IAsaasIntegrationSettingRepository
    {
        public async Task<AsaasIntegrationSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByBranchOrCompanyFallbackAsync(
            long companyId,
            long? branchId,
            CancellationToken cancellationToken = default)
        {
            // 1. Tenta carregar a configuração específica da filial
            if (branchId.HasValue && branchId.Value > 0)
            {
                var branchSetting = await context.Set<AsaasIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.IsActive, cancellationToken);

                if (branchSetting is not null)
                    return branchSetting;
            }

            // 2. Se não houver da filial, pega a padrão da empresa (matriz)
            return await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);
        }

        public async Task<AsaasIntegrationSetting?> GetByScopeAsync(long companyId, long? branchId, CancellationToken cancellationToken = default)
            => branchId.HasValue && branchId.Value > 0
                ? await context.Set<AsaasIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.IsActive, cancellationToken)
                : await context.Set<AsaasIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<IReadOnlyList<AsaasIntegrationSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<AsaasIntegrationSetting>> GetAllActiveByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AnyAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .AnyAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task AddAsync(AsaasIntegrationSetting setting, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSetting>().AddAsync(setting, cancellationToken);

        public void Update(AsaasIntegrationSetting setting)
            => context.Set<AsaasIntegrationSetting>().Update(setting);

        public void Delete(AsaasIntegrationSetting setting)
            => context.Set<AsaasIntegrationSetting>().Remove(setting);
    }
}
