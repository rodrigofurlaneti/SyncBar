using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class ShiftClosingRepository(AppDbContext context) : IShiftClosingRepository
{
    public async Task<ShiftClosing?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.ShiftClosings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ShiftClosing?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.ShiftClosings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<ShiftClosing?> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.ShiftClosings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.IsActive
                && x.ShiftClosingStatusId == ShiftClosingStatusIds.Aberto, cancellationToken);

    public async Task AddAsync(ShiftClosing entity, CancellationToken cancellationToken = default)
        => await context.ShiftClosings.AddAsync(entity, cancellationToken);
}
