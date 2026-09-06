using SyncBar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationOrderEventLogRepository
    {
        Task<KeetaIntegrationOrderEventLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationOrderEventLog?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationOrderEventLog>> GetAllByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationOrderEventLog>> GetUnprocessedEventsAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationOrderEventLog eventLog, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationOrderEventLog eventLog);
        void Delete(KeetaIntegrationOrderEventLog eventLog);
    }
}
