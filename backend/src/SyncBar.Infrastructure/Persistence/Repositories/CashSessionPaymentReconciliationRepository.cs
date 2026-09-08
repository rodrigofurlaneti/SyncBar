using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CashSessionPaymentReconciliationRepository(AppDbContext context) : ICashSessionPaymentReconciliationRepository
{
    public async Task<IReadOnlyCollection<CashSessionPaymentReconciliation>> GetByCashSessionAsync(
        long cashSessionId, CancellationToken cancellationToken = default)
        => await context.CashSessionPaymentReconciliations.AsNoTracking()
            .Where(x => x.CashSessionId == cashSessionId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<CashSessionPaymentReconciliation> entities, CancellationToken cancellationToken = default)
        => await context.CashSessionPaymentReconciliations.AddRangeAsync(entities, cancellationToken);
}
