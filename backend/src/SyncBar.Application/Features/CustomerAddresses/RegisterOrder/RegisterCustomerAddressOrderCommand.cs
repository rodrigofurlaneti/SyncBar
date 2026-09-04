using FluentValidation;
using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.RegisterOrder
{
    public sealed record RegisterCustomerAddressOrderCommand(
        long AddressId,
        long OrderId
    ) : ICommand;
}
