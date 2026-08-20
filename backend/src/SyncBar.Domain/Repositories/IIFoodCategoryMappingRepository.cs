using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodCategoryMappingRepository
{
    Task<IFoodCategoryMapping?> GetByCategoryAndBranchAsync(long categoryId, long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodCategoryMapping entity, CancellationToken cancellationToken = default);
}
