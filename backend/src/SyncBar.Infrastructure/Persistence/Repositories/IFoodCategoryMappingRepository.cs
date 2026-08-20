using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodCategoryMappingRepository(AppDbContext context) : IIFoodCategoryMappingRepository
{
    public async Task<IFoodCategoryMapping?> GetByCategoryAndBranchAsync(long categoryId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodCategoryMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(IFoodCategoryMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodCategoryMappings.AddAsync(entity, cancellationToken);
}
