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
    internal sealed class AsaasIntegrationSavedCardRepository(AppDbContext context) : IAsaasIntegrationSavedCardRepository
    {
        public async Task<AsaasIntegrationSavedCard?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSavedCard?> GetByTokenAsync(string creditCardToken, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CreditCardToken == creditCardToken && x.IsActive, cancellationToken);

        public async Task<IReadOnlyList<AsaasIntegrationSavedCard>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>()
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsByTokenAsync(string creditCardToken, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>()
                .AnyAsync(x => x.CreditCardToken == creditCardToken && x.IsActive, cancellationToken);

        public async Task<AsaasIntegrationSavedCard?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        public async Task AddAsync(AsaasIntegrationSavedCard savedCard, CancellationToken cancellationToken = default)
            => await context.Set<AsaasIntegrationSavedCard>().AddAsync(savedCard, cancellationToken);

        public void Update(AsaasIntegrationSavedCard savedCard)
            => context.Set<AsaasIntegrationSavedCard>().Update(savedCard);

        public void Delete(AsaasIntegrationSavedCard savedCard)
            => context.Set<AsaasIntegrationSavedCard>().Remove(savedCard);
    }
}
