using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICashRegisterRepository
{
    Task<IReadOnlyCollection<CashRegister>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
}
