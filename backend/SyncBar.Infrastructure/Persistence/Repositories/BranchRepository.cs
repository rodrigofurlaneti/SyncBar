using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class BranchRepository(AppDbContext context) : IBranchRepository
{
    public async Task<Branch?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Branchs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Branch?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Branchs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Branch>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Branchs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Branch entity, CancellationToken cancellationToken = default)
        => await context.Branchs.AddAsync(entity, cancellationToken);
}
