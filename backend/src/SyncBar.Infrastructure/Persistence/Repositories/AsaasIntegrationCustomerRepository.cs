using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class AsaasIntegrationCustomerRepository(AppDbContext context) : IAsaasIntegrationCustomerRepository
{
    public async Task<AsaasIntegrationCustomer?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public async Task<AsaasIntegrationCustomer?> GetByCustomerIdAndCompanyIdAsync(
        long customerId,
        long companyId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<AsaasIntegrationCustomer?> GetByCustomerIdAndCompanyIdForUpdateAsync(
        long customerId,
        long companyId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task<AsaasIntegrationCustomer?> GetByAsaasCustomerIdAsync(
        string asaasCustomerId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AsaasCustomerId == asaasCustomerId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyList<AsaasIntegrationCustomer>> GetAllByCompanyIdAsync(
        long companyId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        long customerId,
        long companyId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>()
            .AnyAsync(x => x.CustomerId == customerId && x.CompanyId == companyId && x.IsActive, cancellationToken);

    public async Task AddAsync(AsaasIntegrationCustomer customer, CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationCustomer>().AddAsync(customer, cancellationToken);

    public void Update(AsaasIntegrationCustomer customer)
        => context.Set<AsaasIntegrationCustomer>().Update(customer);

    public void Delete(AsaasIntegrationCustomer customer)
        => context.Set<AsaasIntegrationCustomer>().Remove(customer);
}