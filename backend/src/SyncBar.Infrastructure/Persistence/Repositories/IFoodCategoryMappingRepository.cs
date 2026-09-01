using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodCategoryMappingRepository(AppDbContext context) : IIfoodCategoryMappingRepository
{
    public async Task<IfoodCategoryMapping?> GetByCategoryAndBranchAsync(long categoryId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodCategoryMappings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == categoryId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task AddAsync(IfoodCategoryMapping entity, CancellationToken cancellationToken = default)
        => await context.IfoodCategoryMappings.AddAsync(entity, cancellationToken);
}
