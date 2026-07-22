using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IRevenueTargetRepository
{
    Task<RevenueTarget?> GetByBranchAndMonthAsync(
        long branchId, int referenceYear, int referenceMonth, CancellationToken cancellationToken = default);
    // Tracked — a meta do mes e atualizavel (upsert).
    Task<RevenueTarget?> GetByBranchAndMonthForUpdateAsync(
        long branchId, int referenceYear, int referenceMonth, CancellationToken cancellationToken = default);
    Task AddAsync(RevenueTarget entity, CancellationToken cancellationToken = default);
}
