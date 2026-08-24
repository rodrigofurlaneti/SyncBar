using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PizzaConfigurationRepository(AppDbContext context) : IPizzaConfigurationRepository
{
    public async Task<PizzaConfiguration?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations.AsNoTracking()
            .Include(x => x.Sizes)
            .Include(x => x.Crusts)
            .Include(x => x.Edges)
            .Include(x => x.FlavorPrices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    // Tracked, com filhos — para AddSize/AddCrust/AddEdge/SetFlavorPrice/RemoveFlavor.
    public async Task<PizzaConfiguration?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations
            .Include(x => x.Sizes)
            .Include(x => x.Crusts)
            .Include(x => x.Edges)
            .Include(x => x.FlavorPrices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PizzaConfiguration?> GetByProductIdAsync(long productId, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations.AsNoTracking()
            .Include(x => x.Sizes)
            .Include(x => x.Crusts)
            .Include(x => x.Edges)
            .Include(x => x.FlavorPrices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive, cancellationToken);

    public async Task<PizzaConfiguration?> GetByProductIdForUpdateAsync(long productId, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations
            .Include(x => x.Sizes)
            .Include(x => x.Crusts)
            .Include(x => x.Edges)
            .Include(x => x.FlavorPrices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<PizzaConfiguration>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations.AsNoTracking()
            .Include(x => x.Sizes)
            .Include(x => x.Crusts)
            .Include(x => x.Edges)
            .Include(x => x.FlavorPrices)
            .AsSplitQuery()
            .Where(x => x.IsActive && context.Products.Any(p => p.Id == x.ProductId && p.CompanyId == companyId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PizzaConfiguration entity, CancellationToken cancellationToken = default)
        => await context.PizzaConfigurations.AddAsync(entity, cancellationToken);
}
