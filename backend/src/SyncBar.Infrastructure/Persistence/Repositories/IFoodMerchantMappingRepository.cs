using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodMerchantMappingRepository(AppDbContext context) : IIFoodMerchantMappingRepository
{
    public async Task<IFoodMerchantMapping?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodMerchantMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IFoodMerchantMapping?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodMerchantMappings
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyDictionary<long, IFoodMerchantMapping>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
    {
        var branchIds = await context.Branchs.AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var mappings = await context.IFoodMerchantMappings.AsNoTracking()
            .Where(m => branchIds.Contains(m.BranchId) && m.IsActive)
            .ToListAsync(cancellationToken);

        return mappings.ToDictionary(m => m.BranchId);
    }

    public async Task AddAsync(IFoodMerchantMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodMerchantMappings.AddAsync(entity, cancellationToken);
}
