using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Employee>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee entity, CancellationToken cancellationToken = default);
}
