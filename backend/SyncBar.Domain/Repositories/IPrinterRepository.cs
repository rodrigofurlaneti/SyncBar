using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPrinterRepository
{
    Task<Printer?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Printer?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Printer>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Printer entity, CancellationToken cancellationToken = default);
}
