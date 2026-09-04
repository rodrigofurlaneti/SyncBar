using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.CustomerAddresses.GetByBranchId;
namespace SyncBar.Application.Features.CustomerAddresses.GetByCompanyId
{
    public sealed record GetCustomerAddressesByCompanyIdQuery(long CompanyId) : IQuery<IEnumerable<CustomerAddressResponse>>;
}
