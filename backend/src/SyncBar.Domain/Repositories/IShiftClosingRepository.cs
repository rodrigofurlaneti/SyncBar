using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IShiftClosingRepository
{
    Task<ShiftClosing?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShiftClosing?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<ShiftClosing?> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ShiftClosing>> GetHistoryByBranchAsync(
        long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task AddAsync(ShiftClosing entity, CancellationToken cancellationToken = default);
}
