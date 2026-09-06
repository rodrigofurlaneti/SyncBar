using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class KeetaIntegrationSettingRepository(AppDbContext context) : IKeetaIntegrationSettingRepository
    {
        public async Task<KeetaIntegrationSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == 0, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == branchId, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByBranchOrCompanyFallbackAsync(
            long companyId,
            long? branchId,
            CancellationToken cancellationToken = default)
        {
            // 1. Tenta carregar a configuração específica da filial
            if (branchId.HasValue && branchId.Value > 0)
            {
                var branchSetting = await context.Set<KeetaIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value, cancellationToken);

                if (branchSetting is not null)
                    return branchSetting;
            }

            // 2. Se não houver da filial, pega a padrão da empresa (matriz) assumindo BranchId = 0
            return await context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == 0, cancellationToken);
        }

        public async Task<KeetaIntegrationSetting?> GetByScopeAsync(long companyId, long? branchId, CancellationToken cancellationToken = default)
            => branchId.HasValue && branchId.Value > 0
                ? await context.Set<KeetaIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value, cancellationToken)
                : await context.Set<KeetaIntegrationSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == 0, cancellationToken);

        public async Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .AnyAsync(x => x.CompanyId == companyId && x.BranchId == 0, cancellationToken);

        public async Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .AnyAsync(x => x.BranchId == branchId, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == 0, cancellationToken);

        public async Task<KeetaIntegrationSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>()
                .FirstOrDefaultAsync(x => x.BranchId == branchId, cancellationToken);

        public async Task AddAsync(KeetaIntegrationSetting setting, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationSetting>().AddAsync(setting, cancellationToken);

        public void Update(KeetaIntegrationSetting setting)
            => context.Set<KeetaIntegrationSetting>().Update(setting);

        public void Delete(KeetaIntegrationSetting setting)
            => context.Set<KeetaIntegrationSetting>().Remove(setting);
    }
}
