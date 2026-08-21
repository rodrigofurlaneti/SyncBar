using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ComplementItemRepository(AppDbContext context) : IComplementItemRepository
{
    public async Task<ComplementItem?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.ComplementItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ComplementItem?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.ComplementItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ComplementItem>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.ComplementItems.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ComplementItem>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        => await context.ComplementItems.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ComplementItem entity, CancellationToken cancellationToken = default)
        => await context.ComplementItems.AddAsync(entity, cancellationToken);
}
