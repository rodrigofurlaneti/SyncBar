using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class KeetaIntegrationOrderRepository(AppDbContext context) : IKeetaIntegrationOrderRepository
    {
        public async Task<KeetaIntegrationOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationOrder?> GetByKeetaOrderIdAsync(string keetaOrderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .FirstOrDefaultAsync(x => x.KeetaOrderId == keetaOrderId, cancellationToken);

        public async Task<KeetaIntegrationOrder?> GetByDisplayIdAsync(string displayId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DisplayId == displayId, cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationOrder>> GetAllByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationOrder>> GetAllByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationOrder>> GetActiveOrdersByBranchAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .Where(x => x.BranchId == branchId && x.Status != "CONCLUDED" && x.Status != "CANCELLED")
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByKeetaOrderIdAsync(string keetaOrderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>()
                .AnyAsync(x => x.KeetaOrderId == keetaOrderId, cancellationToken);

        public async Task AddAsync(KeetaIntegrationOrder order, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationOrder>().AddAsync(order, cancellationToken);

        public void Update(KeetaIntegrationOrder order)
            => context.Set<KeetaIntegrationOrder>().Update(order);

        public void Delete(KeetaIntegrationOrder order)
            => context.Set<KeetaIntegrationOrder>().Remove(order);
    }
}
