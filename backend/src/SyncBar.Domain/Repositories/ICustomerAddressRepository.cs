using SyncBar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Domain.Repositories
{
    public interface ICustomerAddressRepository
    {
        Task<CustomerAddress?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<IEnumerable<CustomerAddress>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CustomerAddress>> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CustomerAddress>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<IEnumerable<CustomerAddress>> GetByLastOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
        Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);
        Task UpdateAsync(CustomerAddress address, CancellationToken cancellationToken = default);
        Task RemoveAsync(long id, CancellationToken cancellationToken = default);
    }
}
