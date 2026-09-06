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
    internal sealed class KeetaIntegrationRefundDisputeRepository(AppDbContext context) : IKeetaIntegrationRefundDisputeRepository
    {
        public async Task<KeetaIntegrationRefundDispute?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<KeetaIntegrationRefundDispute?> GetByAfterSaleOrderIdAsync(long afterSaleOrderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>()
                .FirstOrDefaultAsync(x => x.AfterSaleOrderId == afterSaleOrderId, cancellationToken);

        public async Task<KeetaIntegrationRefundDispute?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>()
                .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

        public async Task<IReadOnlyList<KeetaIntegrationRefundDispute>> GetPendingDisputesByBranchAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>()
                .Where(x => x.BranchId == branchId && x.ResolutionStatus == "PENDING")
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByAfterSaleOrderIdAsync(long afterSaleOrderId, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>()
                .AnyAsync(x => x.AfterSaleOrderId == afterSaleOrderId, cancellationToken);

        public async Task AddAsync(KeetaIntegrationRefundDispute dispute, CancellationToken cancellationToken = default)
            => await context.Set<KeetaIntegrationRefundDispute>().AddAsync(dispute, cancellationToken);

        public void Update(KeetaIntegrationRefundDispute dispute)
            => context.Set<KeetaIntegrationRefundDispute>().Update(dispute);
        public void Delete(KeetaIntegrationRefundDispute dispute)
            => context.Set<KeetaIntegrationRefundDispute>().Remove(dispute);
    }
}
