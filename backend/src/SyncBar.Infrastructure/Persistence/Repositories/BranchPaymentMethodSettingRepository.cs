using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class BranchPaymentMethodSettingRepository(AppDbContext context) : IBranchPaymentMethodSettingRepository
    {
        public async Task<BranchPaymentMethodSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByBranchOrCompanyFallbackAsync(
            long companyId,
            long? branchId,
            CancellationToken cancellationToken = default)
        {
            // 1. Tenta carregar a configuração específica da filial
            if (branchId.HasValue && branchId.Value > 0)
            {
                var branchSetting = await context.Set<BranchPaymentMethodSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.IsActive, cancellationToken);

                if (branchSetting is not null)
                    return branchSetting;
            }

            // 2. Se não houver da filial, pega a padrão da empresa (matriz)
            return await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);
        }

        public async Task<BranchPaymentMethodSetting?> GetByScopeAsync(long companyId, long? branchId, CancellationToken cancellationToken = default)
            => branchId.HasValue && branchId.Value > 0
                ? await context.Set<BranchPaymentMethodSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BranchId == branchId.Value && x.IsActive, cancellationToken)
                : await context.Set<BranchPaymentMethodSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<IReadOnlyList<BranchPaymentMethodSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<BranchPaymentMethodSetting>> GetAllActiveByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AnyAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .AnyAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == null && x.IsActive, cancellationToken);

        public async Task<BranchPaymentMethodSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>()
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

        public async Task AddAsync(BranchPaymentMethodSetting setting, CancellationToken cancellationToken = default)
            => await context.Set<BranchPaymentMethodSetting>().AddAsync(setting, cancellationToken);

        public void Update(BranchPaymentMethodSetting setting)
            => context.Set<BranchPaymentMethodSetting>().Update(setting);

        public void Delete(BranchPaymentMethodSetting setting)
            => context.Set<BranchPaymentMethodSetting>().Remove(setting);
    }
}
