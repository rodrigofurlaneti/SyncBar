using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICustomerOrderRepository
{
    Task<CustomerOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    // Tracked, com itens — para AddItem/Close/Cancel.
    Task<CustomerOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerOrder>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CustomerOrder>> GetByBranchAndPeriodAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<bool> HasOpenOrderForTableAsync(long diningTableId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenOrderForComandaAsync(long comandaId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerOrder entity, CancellationToken cancellationToken = default);
}
