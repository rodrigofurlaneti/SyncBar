using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class KeetaIntegrationOrderEventLogRepository(AppDbContext context) : IKeetaIntegrationOrderEventLogRepository
    {
        public async Task<KeetaIntegrationOrderEventLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationOrderEventLog?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>()
                .FirstOrDefaultAsync(x => x.EventId == eventId, cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationOrderEventLog>> GetAllByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>()
                .AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationOrderEventLog>> GetUnprocessedEventsAsync(CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>()
                .Where(x => !x.ProcessedSuccessfully)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>()
                .AnyAsync(x => x.EventId == eventId, cancellationToken);

        public async Task AddAsync(KeetaIntegrationOrderEventLog eventLog, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrderEventLog>().AddAsync(eventLog, cancellationToken);

        public void Update(KeetaIntegrationOrderEventLog eventLog)
            => context.Set<KeetaIntegrationOrderEventLog>().Update(eventLog);

        public void Delete(KeetaIntegrationOrderEventLog eventLog)
            => context.Set<KeetaIntegrationOrderEventLog>().Remove(eventLog);
    }
}
