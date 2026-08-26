using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class WaiterMessageRepository(AppDbContext context) : IWaiterMessageRepository
    {
        public async Task<WaiterMessage?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<WaiterMessage>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<IEnumerable<WaiterMessage>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
                    => await context.Set<WaiterMessage>()
                        .AsNoTracking()
                        .Where(x => x.BranchId == branchId && x.IsActive)
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync(cancellationToken);

        public async Task AddAsync(WaiterMessage entity, CancellationToken cancellationToken = default)
            => await context.Set<WaiterMessage>().AddAsync(entity, cancellationToken);

        public Task UpdateAsync(WaiterMessage entity, CancellationToken cancellationToken = default)
        {
            context.Set<WaiterMessage>().Update(entity);
            return Task.CompletedTask;
        }
    }
}
