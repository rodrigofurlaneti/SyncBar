using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CustomerAppUserRepository(AppDbContext context) : ICustomerAppUserRepository
{
    public async Task<IEnumerable<CustomerAppUser>> GetByCustomerId(long customerId, CancellationToken cancellationToken = default)
    {
        return await context.Set<CustomerAppUser>()
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerAppUser>> GetByBranchId(long branchId, CancellationToken cancellationToken = default)
    {
        return await context.Set<CustomerAppUser>()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerAppUser>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default)
    {
        return await context.Set<CustomerAppUser>()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAppUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Set<CustomerAppUser>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<CustomerAppUser?> GetByEmailForUpdateAsync(string email, long companyId, CancellationToken cancellationToken = default)
    {
        return await context.Set<CustomerAppUser>()
            .FirstOrDefaultAsync(x => x.Email == email && x.CompanyId == companyId && x.IsActive, cancellationToken);
    }

    public Task AddAsync(CustomerAppUser entity, CancellationToken cancellationToken = default)
    {
        context.Set<CustomerAppUser>().Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CustomerAppUser entity, CancellationToken cancellationToken = default)
    {
        context.Set<CustomerAppUser>().Update(entity);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            entity.Deactivate();
            context.Set<CustomerAppUser>().Update(entity);
        }
    }
}