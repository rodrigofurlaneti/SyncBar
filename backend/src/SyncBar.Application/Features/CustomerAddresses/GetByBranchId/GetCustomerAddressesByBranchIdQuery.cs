using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.GetByBranchId
{
    public sealed record GetCustomerAddressesByBranchIdQuery(long BranchId) : IQuery<IEnumerable<CustomerAddressResponse>>;
}
