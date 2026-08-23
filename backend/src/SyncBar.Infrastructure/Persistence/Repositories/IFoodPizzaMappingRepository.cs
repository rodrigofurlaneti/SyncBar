using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodPizzaMappingRepository(AppDbContext context) : IIFoodPizzaMappingRepository
{
    public async Task<IFoodPizzaMapping?> GetByPizzaConfigurationAndBranchAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodPizzaMappings.AsNoTracking()
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.PizzaConfigurationId == pizzaConfigurationId && x.BranchId == branchId && x.IsActive, cancellationToken);

    // Tracked, com Elements — para SetElement/UpdateIFoodPizzaId.
    public async Task<IFoodPizzaMapping?> GetByPizzaConfigurationAndBranchForUpdateAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodPizzaMappings
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.PizzaConfigurationId == pizzaConfigurationId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodPizzaMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodPizzaMappings.AsNoTracking()
            .Include(x => x.Elements)
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodPizzaMapping entity, CancellationToken cancellationToken = default)
        => await context.IFoodPizzaMappings.AddAsync(entity, cancellationToken);
}
