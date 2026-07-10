using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CashSessionRepository(AppDbContext context) : ICashSessionRepository
{
    public async Task<CashSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.CashSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<CashSession?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.CashSessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<CashSession?> GetOpenByCashRegisterAsync(long cashRegisterId, CancellationToken cancellationToken = default)
        => await context.CashSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CashRegisterId == cashRegisterId && x.IsActive
                && x.CashSessionStatusId == CashSessionStatusIds.Aberto, cancellationToken);

    public async Task<IReadOnlyCollection<CashSession>> GetByBranchAndPeriodAsync(
        long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await context.CashSessions.AsNoTracking()
            .Join(context.CashRegisters, s => s.CashRegisterId, r => r.Id, (s, r) => new { s, r })
            .Where(x => x.r.BranchId == branchId && x.s.IsActive && x.s.OpenedAt >= from && x.s.OpenedAt < to)
            .Select(x => x.s)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CashSession entity, CancellationToken cancellationToken = default)
        => await context.CashSessions.AddAsync(entity, cancellationToken);
}
