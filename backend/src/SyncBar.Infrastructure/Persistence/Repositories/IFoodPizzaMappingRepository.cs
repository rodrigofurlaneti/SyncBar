using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodPizzaMappingRepository(AppDbContext context) : IIfoodPizzaMappingRepository
{
    public async Task<IfoodPizzaMapping?> GetByPizzaConfigurationAndBranchAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodPizzaMappings.AsNoTracking()
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.PizzaConfigurationId == pizzaConfigurationId && x.BranchId == branchId && x.IsActive, cancellationToken);

    // Tracked, com Elements — para SetElement/UpdateIfoodPizzaId.
    public async Task<IfoodPizzaMapping?> GetByPizzaConfigurationAndBranchForUpdateAsync(long pizzaConfigurationId, long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodPizzaMappings
            .Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.PizzaConfigurationId == pizzaConfigurationId && x.BranchId == branchId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodPizzaMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodPizzaMappings.AsNoTracking()
            .Include(x => x.Elements)
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodPizzaMapping entity, CancellationToken cancellationToken = default)
        => await context.IfoodPizzaMappings.AddAsync(entity, cancellationToken);
}
