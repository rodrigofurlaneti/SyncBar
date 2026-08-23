using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PizzaFlavorRepository(AppDbContext context) : IPizzaFlavorRepository
{
    public async Task<PizzaFlavor?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.PizzaFlavors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PizzaFlavor?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.PizzaFlavors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<PizzaFlavor>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.PizzaFlavors.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<PizzaFlavor>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        => await context.PizzaFlavors.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PizzaFlavor entity, CancellationToken cancellationToken = default)
        => await context.PizzaFlavors.AddAsync(entity, cancellationToken);
}
