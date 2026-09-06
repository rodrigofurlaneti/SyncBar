using SyncBar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationOrderRepository
    {
        Task<KeetaIntegrationOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationOrder?> GetByKeetaOrderIdAsync(string keetaOrderId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationOrder?> GetByDisplayIdAsync(string displayId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationOrder>> GetAllByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationOrder>> GetAllByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationOrder>> GetActiveOrdersByBranchAsync(long branchId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByKeetaOrderIdAsync(string keetaOrderId, CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationOrder order, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationOrder order);
        void Delete(KeetaIntegrationOrder order);
    }
}
