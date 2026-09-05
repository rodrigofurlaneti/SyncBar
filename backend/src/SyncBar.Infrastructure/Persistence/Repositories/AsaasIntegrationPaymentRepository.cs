using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class AsaasIntegrationPaymentRepository(AppDbContext context) : IAsaasIntegrationPaymentRepository
    {
        public async Task<AsaasIntegrationPayment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationPayment?> GetByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AsaasPaymentId == asaasPaymentId && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationPayment?> GetByCustomerOrderIdAsync(long customerOrderId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerOrderId == customerOrderId && x.IsActive, cancellationToken);

        public async Task<IReadOnlyList<AsaasIntegrationPayment>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .Where(x => x.BranchId == branchId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<AsaasIntegrationPayment>> GetPendingByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AsNoTracking()
                .Where(x => x.BranchId == branchId && x.Status == "PENDING" && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .AnyAsync(x => x.AsaasPaymentId == asaasPaymentId && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationPayment?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationPayment?> GetByAsaasPaymentIdForUpdateAsync(string asaasPaymentId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>()
                .FirstOrDefaultAsync(x => x.AsaasPaymentId == asaasPaymentId && x.IsActive, cancellationToken);

        public async Task AddAsync(AsaasIntegrationPayment payment, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationPayment>().AddAsync(payment, cancellationToken);

        public void Update(AsaasIntegrationPayment payment)
            => context.Set<AsaasIntegrationPayment>().Update(payment);

        public void Delete(AsaasIntegrationPayment payment)
            => context.Set<AsaasIntegrationPayment>().Remove(payment);
    }
}
