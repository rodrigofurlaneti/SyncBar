using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.CustomerAddresses.Remove
{
    public sealed record RemoveCustomerAddressCommand(long Id) : ICommand;
}
