using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.GetById
{
    public sealed record GetCustomerAddressByIdQuery(long Id) : IQuery<CustomerAddressResponse>;
}
