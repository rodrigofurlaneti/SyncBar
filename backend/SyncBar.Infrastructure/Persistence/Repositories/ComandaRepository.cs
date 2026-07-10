using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ComandaRepository(AppDbContext context) : IComandaRepository
{
    public async Task<Comanda?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Comandas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Comanda?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Comandas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Comanda?> GetByCodeAsync(long branchId, string code, CancellationToken cancellationToken = default)
        => await context.Comandas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.Code == code && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<Comanda>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Comandas.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Comanda entity, CancellationToken cancellationToken = default)
        => await context.Comandas.AddAsync(entity, cancellationToken);
}
