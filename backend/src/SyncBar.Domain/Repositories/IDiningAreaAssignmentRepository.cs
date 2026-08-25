using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IDiningAreaAssignmentRepository
    {
        Task<DiningAreaAssignment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<DiningAreaAssignment>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DiningAreaAssignment>> GetActiveByDiningAreaIdAsync(long diningAreaId, CancellationToken cancellationToken = default);
        Task AddAsync(DiningAreaAssignment entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(DiningAreaAssignment entity, CancellationToken cancellationToken = default);
    }
}