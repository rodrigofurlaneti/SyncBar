using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class KeetaIntegrationMerchantMappingRepository(AppDbContext context) : IKeetaIntegrationMerchantMappingRepository
    {
        public async Task<KeetaIntegrationMerchantMapping?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationMerchantMapping?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationMerchantMapping?> GetByKeetaMerchantIdAsync(long keetaMerchantId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.KeetaMerchantId == keetaMerchantId, cancellationToken);

        public async Task<KeetaIntegrationMerchantMapping?> GetByInternalMerchantIdAsync(string internalMerchantId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InternalMerchantId == internalMerchantId, cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationMerchantMapping>> GetAllByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationMerchantMapping>> GetAllByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByKeetaMerchantIdAsync(long keetaMerchantId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>()
                .AnyAsync(x => x.KeetaMerchantId == keetaMerchantId, cancellationToken);

        public async Task AddAsync(KeetaIntegrationMerchantMapping mapping, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationMerchantMapping>().AddAsync(mapping, cancellationToken);

        public void Update(KeetaIntegrationMerchantMapping mapping)
            => context.Set<KeetaIntegrationMerchantMapping>().Update(mapping);

        public void Delete(KeetaIntegrationMerchantMapping mapping)
            => context.Set<KeetaIntegrationMerchantMapping>().Remove(mapping);
    }
}
