using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICashSessionPaymentReconciliationRepository
{
    Task<IReadOnlyCollection<CashSessionPaymentReconciliation>> GetByCashSessionAsync(
        long cashSessionId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<CashSessionPaymentReconciliation> entities, CancellationToken cancellationToken = default);
}
