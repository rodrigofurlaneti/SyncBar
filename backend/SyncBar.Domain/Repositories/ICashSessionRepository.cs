using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICashSessionRepository
{
    Task<CashSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CashSession?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<CashSession?> GetOpenByCashRegisterAsync(long cashRegisterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CashSession>> GetByBranchAndPeriodAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddAsync(CashSession entity, CancellationToken cancellationToken = default);
}
