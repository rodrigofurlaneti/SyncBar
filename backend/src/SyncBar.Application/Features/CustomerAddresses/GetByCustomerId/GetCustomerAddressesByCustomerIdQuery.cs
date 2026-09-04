using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.GetByCustomerId
{
    public sealed record GetCustomerAddressesByCustomerIdQuery(long CustomerId) : IQuery<IEnumerable<CustomerAddressResponse>>;
}
