using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ComplementGroupRepository(AppDbContext context) : IComplementGroupRepository
{
    public async Task<ComplementGroup?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.ComplementGroups.AsNoTracking()
            .Include(x => x.Complements)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    // Tracked, com Complements — para AddComplement/RemoveComplement/UpdateDetails.
    public async Task<ComplementGroup?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.ComplementGroups
            .Include(x => x.Complements)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ComplementGroup>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.ComplementGroups.AsNoTracking()
            .Include(x => x.Complements)
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ComplementGroup>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        => await context.ComplementGroups.AsNoTracking()
            .Include(x => x.Complements)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ComplementGroup entity, CancellationToken cancellationToken = default)
        => await context.ComplementGroups.AddAsync(entity, cancellationToken);
}
