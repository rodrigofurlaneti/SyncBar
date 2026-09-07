using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IOrderOriginRepository
    {
        Task<OrderOrigin?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        Task<OrderOrigin?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OrderOrigin>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OrderOrigin>> GetByCompanyAndBranchAsync(long? companyId, long? branchId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<OrderOrigin>> GetFilteredAsync(
            long? companyId,
            long? branchId,
            string? searchTerm,
            bool? isActive,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(long? companyId, long? branchId, string name, long? excludeId = null, CancellationToken cancellationToken = default);

        Task AddAsync(OrderOrigin entity, CancellationToken cancellationToken = default);

        void Update(OrderOrigin entity);

        void Remove(OrderOrigin entity);
    }
}
