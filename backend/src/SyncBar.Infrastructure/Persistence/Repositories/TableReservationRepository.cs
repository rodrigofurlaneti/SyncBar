using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class TableReservationRepository(AppDbContext context) : ITableReservationRepository
{
    public async Task<TableReservation?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.TableReservations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<TableReservation>> GetByBranchAndDateAsync(
        long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await context.TableReservations.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.ReservedFor >= from && x.ReservedFor < to)
            .OrderBy(x => x.ReservedFor)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TableReservation entity, CancellationToken cancellationToken = default)
        => await context.TableReservations.AddAsync(entity, cancellationToken);
}
