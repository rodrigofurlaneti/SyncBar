using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodMerchantMappingRepository(AppDbContext context) : IIfoodMerchantMappingRepository
{
    public async Task<IfoodMerchantMapping?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodMerchantMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IfoodMerchantMapping?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodMerchantMappings
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyDictionary<long, IfoodMerchantMapping>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
    {
        var branchIds = await context.Branchs.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var mappings = await context.IfoodMerchantMappings.AsNoTracking()
            .Where(m => branchIds.Contains(m.BranchId) && m.IsActive)
            .ToListAsync(cancellationToken);

        return mappings.ToDictionary(m => m.BranchId);
    }

    public async Task AddAsync(IfoodMerchantMapping entity, CancellationToken cancellationToken = default)
        => await context.IfoodMerchantMappings.AddAsync(entity, cancellationToken);
}
