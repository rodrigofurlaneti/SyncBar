using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Customer?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Customer>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Customers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Customer>> SearchAsync(long companyId, string term, CancellationToken cancellationToken = default)
        => await context.Customers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive &&
                (x.Name.Contains(term) || (x.Phone != null && x.Phone.Contains(term))))
            .OrderBy(x => x.Name)
            .Take(20)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Customer entity, CancellationToken cancellationToken = default)
        => await context.Customers.AddAsync(entity, cancellationToken);
}
