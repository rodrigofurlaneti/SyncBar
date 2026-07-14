using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IDiningTableRepository
{
    Task<DiningTable?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<DiningTable?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DiningTable>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    // Autoatendimento via QR Code — resolve a mesa a partir do token público do link/QR.
    Task<DiningTable?> GetByQrTokenAsync(Guid token, CancellationToken cancellationToken = default);
    Task AddAsync(DiningTable entity, CancellationToken cancellationToken = default);
}
