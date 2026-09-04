using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Repositories
{
    internal sealed class CustomerAddressRepository(AppDbContext context) : ICustomerAddressRepository
    {
        public async Task<CustomerAddress?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await context.Set<CustomerAddress>()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<CustomerAddress>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default)
        {
            return await context.Set<CustomerAddress>()
                .Where(x => x.CustomerId == customerId && x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerAddress>> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default)
        {
            return await context.Set<CustomerAddress>()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerAddress>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default)
        {
            return await context.Set<CustomerAddress>()
                .Where(x => x.BranchId == branchId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CustomerAddress>> GetByLastOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
        {
            return await context.Set<CustomerAddress>()
                .Where(x => x.LastOrderId == orderId && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default)
        {
            await context.Set<CustomerAddress>().AddAsync(address, cancellationToken);
        }

        public Task UpdateAsync(CustomerAddress address, CancellationToken cancellationToken = default)
        {
            context.Set<CustomerAddress>().Update(address);
            return Task.CompletedTask;
        }

        public async Task RemoveAsync(long id, CancellationToken cancellationToken = default)
        {
            var address = await GetByIdAsync(id, cancellationToken);
            if (address is not null)
            {
                address.Deactivate();
                context.Set<CustomerAddress>().Update(address);
            }
        }
    }
}